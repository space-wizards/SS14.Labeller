using Microsoft.Extensions.FileSystemGlobbing;
using SS14.Labeller.Configuration;
using SS14.Labeller.DiscourseApi;
using SS14.Labeller.GitHubApi;
using SS14.Labeller.Labels;
using SS14.Labeller.Messages;
using SS14.Labeller.Models;

namespace SS14.Labeller.Handlers;

public class LabelPullRequestHandler : RequestHandlerBase<PullRequestEvent>
{
    private readonly IGitHubApiClient _client;
    private readonly IDiscourseClient _discourseClient;
    private readonly DiscourseConfig _discourseConfig = new();

    public LabelPullRequestHandler(IGitHubApiClient client, IDiscourseClient discourseClient, IConfiguration configuration)
    {
        _client = client;
        _discourseClient = discourseClient;

        configuration.Bind(DiscourseConfig.Name, _discourseConfig);
    }

    /// <inheritdoc />
    public override string EventType => "pull_request";

    /// <inheritdoc />
    protected override async Task HandleInternal(PullRequestEvent request, CancellationToken ct)
    {
        var pr = request.PullRequest;

        var number = pr.Number;
        var labels = pr.Labels
                       .Select(x => x.Name)
                       .ToArray();

        // basic labels
        var repository = request.Repository;

        if (request.Action is "opened")
        {
            if (labels.Length == 0)
                await _client.AddLabel(repository, number, StatusLabels.Untriaged, ct);

            var targetBranch = pr.Base.Ref;
            if (targetBranch == "stable" && !labels.Contains(BranchLabels.Stable))
                await _client.AddLabel(repository, number, BranchLabels.Stable, ct);
            else if (targetBranch == "staging" && !labels.Contains(BranchLabels.Staging))
                await _client.AddLabel(repository, number, BranchLabels.Staging, ct);

            var permission = await _client.GetPermission(repository, pr.User.Login, ct);
            if (permission is "write" or "admin")
                await _client.AddLabel(repository, number, StatusLabels.Approved, ct);

            await _client.AddLabel(repository, number, StatusLabels.RequireReview, ct);
        }

        if (request.Action is "synchronize" or "opened")
        {
            var totalDiff = pr.Additions + pr.Deletions;
            var sizeLabel = SizeLabels.TryGetLabelFor(totalDiff);

            // remove the existing size/* labels
            foreach (var label in labels)
            {
                if (label == sizeLabel)
                    continue; // Don't remove a label that is accurate

                if (label?.StartsWith(SizeLabels.Prefix, StringComparison.OrdinalIgnoreCase) == true)
                {
                    await _client.RemoveLabel(repository, number, label, ct);
                }
            }

            if (sizeLabel is not null && !labels.Contains(sizeLabel))
            {
                await _client.AddLabel(repository, number, sizeLabel, ct);
            }
        }

        if (request.Action is "labeled")
        {
            // ReSharper disable once NullableWarningSuppressionIsUsed
            if (request.Label!.Name == StatusLabels.UndergoingDiscussion)
            {
                // We are making a discussion, yipee!

                // we get all comments to see if we already have made a discussion thread before
                var comments = await _client.GetComments(repository, number, ct);

                if (!comments.Any(x => x.Body.StartsWith(StatusMessages.StartedDiscussion)))
                {
                    // we need to make a new thread!
                    var topic = await _discourseClient.CreateTopic(
                            _discourseConfig.DiscussionCategoryId,
                        StatusMessages.DiscourseTopicBody
                            .Replace("{link}", request.PullRequest.Url),
                        request.PullRequest.Title,
                        ct);

                    var topicLink = _discourseConfig.Url + topic[1..];

                    await _client.AddComment(repository, number, StatusMessages.StartedDiscussion + topicLink, ct);
                }
            }
        }

        if (request.Action is "review_requested")
        {
            // ReSharper disable once NullableWarningSuppressionIsUsed - Asssuming review_requested, there should always be a requested reviewer.
            var requestedPermission = await _client.GetPermission(repository, request.RequestedReviewer!.Login, ct);

            if (labels.Contains(StatusLabels.AwaitingChanges) && requestedPermission is "write" or "admin")
            {
                await _client.AddLabel(repository, number, StatusLabels.RequireReview, ct);
                await _client.RemoveLabel(repository, number, StatusLabels.AwaitingChanges, ct);
            }
        }

        if (request.Action is "closed" && !string.IsNullOrEmpty(request.PullRequest.MergedAt))
        { // PR got merged
            if (labels.Contains(StatusLabels.Untriaged))
            {
                await _client.AddComment(repository, number, StatusMessages.UntriagedPullRequestMergedComment, ct);
            }
        }

        var changedFiles = await _client.GetChangedFiles(repository, number, ct);

        var sprites = new Matcher().AddInclude("**/*.rsi/*.png");
        if (sprites.Match(changedFiles).HasMatches)
            await _client.AddLabel(repository, number, ChangesLabels.Sprites, ct);

        var maps = new Matcher().AddInclude("Resources/Maps/**/*.yml")
                                .AddInclude("Resources/Prototypes/Maps/**/*.yml");
        if (maps.Match(changedFiles).HasMatches)
            await _client.AddLabel(repository, number, ChangesLabels.Map, ct);
        else
            await RemoveLabelIfApplied(ChangesLabels.Map);

        var ui =      new Matcher().AddInclude("**/*.xaml*");
        if (ui.Match(changedFiles).HasMatches)
            await _client.AddLabel(repository, number, ChangesLabels.Ui, ct);
        else
            await RemoveLabelIfApplied(ChangesLabels.Ui);

        var shaders = new Matcher().AddInclude("**/*.swsl");
        if (shaders.Match(changedFiles).HasMatches)
            await _client.AddLabel(repository, number, ChangesLabels.Shaders, ct);
        else
            await RemoveLabelIfApplied(ChangesLabels.Shaders);

        var audio =   new Matcher().AddInclude("**/*.ogg");
        if (audio.Match(changedFiles).HasMatches)
            await _client.AddLabel(repository, number, ChangesLabels.Audio, ct);
        else
            await RemoveLabelIfApplied(ChangesLabels.Audio);

        var cs = new Matcher().AddInclude("**/*.cs");
        if (!cs.Match(changedFiles).HasMatches)
            await _client.AddLabel(repository, number, ChangesLabels.NoCSharp, ct);
        else
            await RemoveLabelIfApplied(ChangesLabels.NoCSharp);

        return;

        async Task RemoveLabelIfApplied(string label)
        {
            if (!labels.Contains(label))
                return;

            await _client.RemoveLabel(repository, number, label, ct);
        }
    }
}