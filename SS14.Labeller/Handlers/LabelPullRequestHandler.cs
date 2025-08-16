using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Options;
using SS14.Labeller.Configuration;
using SS14.Labeller.DiscourseApi;
using SS14.Labeller.GitHubApi;
using SS14.Labeller.Labelling;
using SS14.Labeller.Labelling.Labels;
using SS14.Labeller.Messages;
using SS14.Labeller.Models;
using SS14.Labeller.Repository;

namespace SS14.Labeller.Handlers;

public class LabelPullRequestHandler(
    IGitHubApiClient client,
    IDiscourseClient discourseClient,
    IDiscourseTopicsRepository topicsRepository,
    ILabelManager labelManager,
    IOptions<DiscourseConfig> config
) : RequestHandlerBase<PullRequestEvent>
{
    private readonly DiscourseConfig _discourseConfig = config.Value;

    /// <inheritdoc />
    public override string EventType => "pull_request";

    /// <inheritdoc />
    protected override async Task HandleInternal(PullRequestEvent request, CancellationToken ct)
    {
        var pr = request.PullRequest;

        var prNumber = pr.Number;

        var repoOwner = request.Repository.Owner.Login;
        var repoName = request.Repository.Name;

        var labels = pr.Labels
                       .Select(x => x.Name)
                       .ToArray();

        // basic labels
        var repository = request.Repository;

        if (request.Action is "opened")
        {
            if (labels.Length == 0)
                await labelManager.EnsureLabeled(request, StatusLabel.Untriaged, ct);

            var targetBranch = pr.Base.Ref;
            if (targetBranch == "stable")
                await labelManager.EnsureLabeled(request, BranchLabel.Stable, ct);
            else if (targetBranch == "staging")
                await labelManager.EnsureLabeled(request, BranchLabel.Staging, ct);

            var isMaintainer = await client.IsMaintainer(pr.User.Login, repository, ct);
            if (isMaintainer)
                await labelManager.EnsureLabeled(request, StatusLabel.Approved, ct);

            await labelManager.EnsureLabeled(request, StageOfWorkLabel.RequireReview, ct);
        }

        if (request.Action is "synchronize" or "opened")
        {
            var totalDiff = pr.Additions + pr.Deletions;
            var sizeLabel = SizeLabel.TryGetLabelFor(totalDiff);
            if (sizeLabel is not null)
            {
                await labelManager.EnsureLabeled(request, sizeLabel, ct);
            }
        }

        if (request.Action is "labeled")
        {
            // ReSharper disable once NullableWarningSuppressionIsUsed
            if (request.Label!.Name == StatusLabel.UndergoingDiscussion && _discourseConfig.Enable)
            {
                var exists = await topicsRepository.HasTopic(repoOwner, repoName, prNumber, ct);
                if (exists)
                {
                    // need to make a new discussion.
                    var topic = await discourseClient.CreateTopic(
                        _discourseConfig.DiscussionCategoryId,
                        StatusMessages.DiscourseTopicBody
                                      .Replace("{link}", request.PullRequest.Url),
                        request.PullRequest.Title,
                        ct);

                    var topicLink = _discourseConfig.Url + topic.PostUrl[1..];

                    await client.AddComment(repository, prNumber, StatusMessages.StartedDiscussion + topicLink, ct);


                    await discourseClient.ApplyTags(topic.TopicId, ct, _discourseConfig.Tagging.PrOpenTag);

                    await topicsRepository.Add(repoOwner, repoName, prNumber, topic.TopicId, ct);
                }
            }
        }

        if (request.Action is "review_requested")
        {
            if (await client.IsMaintainer(request.RequestedReviewer!.Login, repository, ct))
            {
                await labelManager.EnsureLabeled(request, StageOfWorkLabel.RequireReview, ct);
            }
        }

        if (request.Action is "closed" && !string.IsNullOrEmpty(request.PullRequest.MergedAt))
        {
            // PR got merged
            var discussion = await topicsRepository.FindTopicIdForDiscussion(repoOwner, repoName, prNumber, ct);

            if (discussion is not null)
            {
                // we have an active discussion, lets mark it as doneso
                await discourseClient.ApplyTags(discussion.Value, ct, _discourseConfig.Tagging.PrMergedTag);
            }

            if (labels.Contains(StatusLabel.Untriaged))
            {
                await client.AddComment(repository, prNumber, StatusMessages.UntriagedPullRequestMergedComment, ct);
            }
        }
        else if (request.Action is "closed")
        {
            // pr was just closed, not merged.
            var discussion =
                await topicsRepository.FindTopicIdForDiscussion(repoOwner, repoName, prNumber, ct);

            if (discussion is not null)
            {
                await discourseClient.ApplyTags(discussion.Value, ct, _discourseConfig.Tagging.PrClosedTag);
            }
        }

        var changedFiles = await client.GetChangedFiles(repository, prNumber, ct);

        await EnsureChangesLabels(ChangesLabel.Sprites, ["**/*.rsi/*.png"], request, changedFiles, ct: ct);
        await EnsureChangesLabels(ChangesLabel.Map, ["Resources/Maps/**/*.yml", "Resources/Prototypes/Maps/**/*.yml"], request, changedFiles, ct: ct);
        await EnsureChangesLabels(ChangesLabel.Ui, ["**/*.xaml*"], request, changedFiles, ct:ct);
        await EnsureChangesLabels(ChangesLabel.Shaders, ["**/*.sws"], request, changedFiles, ct: ct);
        await EnsureChangesLabels(ChangesLabel.Audio, ["**/*.ogg"], request, changedFiles, ct: ct);
        await EnsureChangesLabels(ChangesLabel.NoCSharp, ["**/*.cs"], request, changedFiles, isInverted: true, ct: ct);
    }

    private async Task EnsureChangesLabels(
        ChangesLabel label,
        string[] patterns,
        PullRequestEvent request,
        List<string> changedFiles,
        bool isInverted = false,
        CancellationToken ct = default
    )
    {
        var matcher = new Matcher();
        foreach (var pattern in patterns)
        {
            matcher = matcher.AddInclude(pattern);
        }

        if ((matcher.Match(changedFiles).HasMatches && !isInverted) || (!matcher.Match(changedFiles).HasMatches && isInverted))
            await labelManager.EnsureLabeled(request, label, ct);
        else
            await labelManager.EnsureNotLabeled(request, label, ct);
    }
}