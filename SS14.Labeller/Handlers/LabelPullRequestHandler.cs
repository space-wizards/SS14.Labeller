using Dapper;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Options;
using SS14.Labeller.Configuration;
using SS14.Labeller.Database;
using SS14.Labeller.DiscourseApi;
using SS14.Labeller.GitHubApi;
using SS14.Labeller.Labels;
using SS14.Labeller.Messages;
using SS14.Labeller.Models;

namespace SS14.Labeller.Handlers;

public class LabelPullRequestHandler(
    IGitHubApiClient client,
    IDiscourseClient discourseClient,
    IOptions<DiscourseConfig> config,
    DataManager dataManager
) : RequestHandlerBase<PullRequestEvent>
{
    private readonly DiscourseConfig _discourseConfig = config.Value;

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
                await client.AddLabel(repository, number, StatusLabels.Untriaged, ct);

            var targetBranch = pr.Base.Ref;
            if (targetBranch == "stable" && !labels.Contains(BranchLabels.Stable))
                await client.AddLabel(repository, number, BranchLabels.Stable, ct);
            else if (targetBranch == "staging" && !labels.Contains(BranchLabels.Staging))
                await client.AddLabel(repository, number, BranchLabels.Staging, ct);

            var permission = await client.GetPermission(repository, pr.User.Login, ct);
            if (permission is "write" or "admin")
                await client.AddLabel(repository, number, StatusLabels.Approved, ct);

            await client.AddLabel(repository, number, StatusLabels.RequireReview, ct);
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
                    await client.RemoveLabel(repository, number, label, ct);
                }
            }

            if (sizeLabel is not null && !labels.Contains(sizeLabel))
            {
                await client.AddLabel(repository, number, sizeLabel, ct);
            }
        }

        if (request.Action is "labeled")
        {
            // ReSharper disable once NullableWarningSuppressionIsUsed
            if (request.Label!.Name == StatusLabels.UndergoingDiscussion && _discourseConfig.Enable)
            {
                // We are making a discussion, yipee!
                await using var connection = dataManager.OpenConnection();
                const string sql = """
                                       SELECT 1 FROM Discussions
                                       WHERE RepoOwner = @Owner AND RepoName = @Name AND IssueNumber = @Number
                                       LIMIT 1;
                                   """;

                var exists = await connection.ExecuteScalarAsync<int?>(sql, new
                {
                    Owner = request.Repository.Owner.Login,
                    Name = request.Repository.Name,
                    Number = request.PullRequest.Number
                });

                if (exists is null)
                { // need to make a new discussion.
                    var topic = await discourseClient.CreateTopic(
                        _discourseConfig.DiscussionCategoryId,
                        StatusMessages.DiscourseTopicBody
                            .Replace("{link}", request.PullRequest.Url),
                        request.PullRequest.Title,
                        ct);

                    var topicLink = _discourseConfig.Url + topic.PostUrl[1..];

                    await client.AddComment(repository, number, StatusMessages.StartedDiscussion + topicLink, ct);

                    const string insert = """
                                              INSERT INTO Discussions (RepoOwner, RepoName, IssueNumber, TopicId)
                                              VALUES (@Owner, @Name, @Number, @TopicId);
                                          """;

                    await discourseClient.ApplyTags(topic.TopicId, ct, _discourseConfig.Tagging.PrOpenTag);

                    await connection.ExecuteAsync(insert, new
                    {
                        Owner = request.Repository.Owner.Login,
                        Name = request.Repository.Name,
                        Number = request.PullRequest.Number,
                        TopicId = topic.TopicId
                    });
                }
            }
        }

        if (request.Action is "review_requested")
        {
            // ReSharper disable once NullableWarningSuppressionIsUsed - Asssuming review_requested, there should always be a requested reviewer.
            var requestedPermission = await client.GetPermission(repository, request.RequestedReviewer!.Login, ct);

            if (labels.Contains(StatusLabels.AwaitingChanges) && requestedPermission is "write" or "admin")
            {
                await client.AddLabel(repository, number, StatusLabels.RequireReview, ct);
                await client.RemoveLabel(repository, number, StatusLabels.AwaitingChanges, ct);
            }
        }

        if (request.Action is "closed" && !string.IsNullOrEmpty(request.PullRequest.MergedAt))
        { // PR got merged
            var discussion =
                await dataManager.GetTopicIdForDiscussion(request.Repository.Owner.Login, request.Repository.Name,
                    number);

            if (discussion is not null)
            {
                // we have an active discussion, lets mark it as doneso
                await discourseClient.ApplyTags(discussion.Value, ct, _discourseConfig.Tagging.PrMergedTag);
            }

            if (labels.Contains(StatusLabels.Untriaged))
            {
                await client.AddComment(repository, number, StatusMessages.UntriagedPullRequestMergedComment, ct);
            }
        } else if (request.Action is "closed")
        {
            // pr was just closed, not merged.
            var discussion =
                await dataManager.GetTopicIdForDiscussion(request.Repository.Owner.Login, request.Repository.Name,
                    number);

            if (discussion is not null)
            {
                await discourseClient.ApplyTags(discussion.Value, ct, _discourseConfig.Tagging.PrClosedTag);
            }
        }

        var changedFiles = await client.GetChangedFiles(repository, number, ct);

        var sprites = new Matcher().AddInclude("**/*.rsi/*.png");
        if (sprites.Match(changedFiles).HasMatches)
            await client.AddLabel(repository, number, ChangesLabels.Sprites, ct);

        var maps = new Matcher().AddInclude("Resources/Maps/**/*.yml")
                                .AddInclude("Resources/Prototypes/Maps/**/*.yml");
        if (maps.Match(changedFiles).HasMatches)
            await client.AddLabel(repository, number, ChangesLabels.Map, ct);
        else
            await RemoveLabelIfApplied(ChangesLabels.Map);

        var ui =      new Matcher().AddInclude("**/*.xaml*");
        if (ui.Match(changedFiles).HasMatches)
            await client.AddLabel(repository, number, ChangesLabels.Ui, ct);
        else
            await RemoveLabelIfApplied(ChangesLabels.Ui);

        var shaders = new Matcher().AddInclude("**/*.swsl");
        if (shaders.Match(changedFiles).HasMatches)
            await client.AddLabel(repository, number, ChangesLabels.Shaders, ct);
        else
            await RemoveLabelIfApplied(ChangesLabels.Shaders);

        var audio =   new Matcher().AddInclude("**/*.ogg");
        if (audio.Match(changedFiles).HasMatches)
            await client.AddLabel(repository, number, ChangesLabels.Audio, ct);
        else
            await RemoveLabelIfApplied(ChangesLabels.Audio);

        var cs = new Matcher().AddInclude("**/*.cs");
        if (!cs.Match(changedFiles).HasMatches)
            await client.AddLabel(repository, number, ChangesLabels.NoCSharp, ct);
        else
            await RemoveLabelIfApplied(ChangesLabels.NoCSharp);

        return;

        async Task RemoveLabelIfApplied(string label)
        {
            if (!labels.Contains(label))
                return;

            await client.RemoveLabel(repository, number, label, ct);
        }
    }
}