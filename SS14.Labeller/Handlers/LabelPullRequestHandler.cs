using System.Text.RegularExpressions;
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

public partial class LabelPullRequestHandler(
    IGitHubApiClient client,
    IDiscourseClient discourseClient,
    IDiscourseTopicsRepository topicsRepository,
    ILabelManager labelManager,
    IOptions<DiscourseConfig> config
) : RequestHandlerBase<PullRequestEvent>
{
    private readonly DiscourseConfig _discourseConfig = config.Value;

    // Regex explanation:
    // ^ start of new line
    // ## markdown header symbols
    // \s+ at least one whitespace character
    // Breaking Changes
    // \s* optional white space
    // \r?\n line break
    // (?<breakingChanges>.*?) Capture everything inside the section, captured group is named 'breakingChanges'
    // (?=^##\s|^#\s|\Z) stops when
    //   ^## next section or
    //   ^# next higher level section or
    //   **Changelog** backwards compability for the previously used section header
    //   \z end of text
    [GeneratedRegex(@"^##\s+Breaking Changes\s*\r?\n(?<breakingChanges>.*?)(?=^##\s|^#\s|^\*\*Changelog\*\*|\z)", RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.IgnoreCase)]
    private static partial Regex BreakingChangesRegex();

    /// <summary>
    /// Regex for removing markdown comments before parsing the breaking changes section.
    /// </summary>
    [GeneratedRegex(@"<!--.*?-->", RegexOptions.Singleline)]
    private static partial Regex MarkdownCommentRemovalRegex();

    /// <summary>
    /// Matches any word character to detect meaningful text content,
    /// as opposed to only punctuation/whitespace/special symbols.
    /// </summary>
    [GeneratedRegex(@"\w")]
    private static partial Regex MeaningfulContentRegex();

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

        await (request.EventType switch
        {
            PullRequestEventType.Labelled => OnLabelAdd(request, ct, repoOwner, repoName, prNumber, repository),
            PullRequestEventType.ClosedRejected => OnClosed(request, ct, repoOwner, repoName, prNumber),
            PullRequestEventType.ClosedMerged => OnMerged(request, ct, repoOwner, repoName, prNumber, labels, repository),
            PullRequestEventType.Opened => OnOpened(request, ct, labels, pr, repository),
            PullRequestEventType.ReviewRequested => OnReviewRequested(request, ct, repository),
            _ => Task.CompletedTask
        });

        var totalDiff = pr.Additions + pr.Deletions;
        if (SizeLabel.TryGetLabelFor(totalDiff, out var sizeLabel))
        {
            await labelManager.EnsureLabeled(request, sizeLabel, ct);
        }

        if(!ContainsLabelsStartingWith(labels, "A:", "T:", "P:"))
            await labelManager.EnsureLabeled(request, StatusLabel.Untriaged, ct);

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

        // either we found and it is not inverted, or we did not and it is inverted
        if ((matcher.Match(changedFiles).HasMatches && !isInverted) || (!matcher.Match(changedFiles).HasMatches && isInverted))
            await labelManager.EnsureLabeled(request, label, ct);
        else
            await labelManager.EnsureNotLabeled(request, label, ct);
    }

    private async Task OnClosed(PullRequestEvent request, CancellationToken ct, string repoOwner, string repoName, int prNumber)
    {
        // pr was just closed, not merged.
        var discussion = await topicsRepository.FindTopicIdForDiscussion(repoOwner, repoName, prNumber, ct);

        if (discussion is not null)
        {
            await discourseClient.ApplyTags(discussion.Value, ct, _discourseConfig.Tagging.PrClosedTag);
        }
    }

    private async Task OnMerged(PullRequestEvent request, CancellationToken ct, string repoOwner, string repoName, int prNumber, string?[] labels, GithubRepo repository)
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

        if(!_discourseConfig.Enable)
            return;

        var prBody = request.PullRequest.Body;

        if (string.IsNullOrWhiteSpace(prBody))
            return;

        // Remove markdown comments.
        prBody = MarkdownCommentRemovalRegex().Replace(prBody, string.Empty);

        // Match the breaking changes section.
        var match = BreakingChangesRegex().Match(prBody);

        if (!match.Success || !match.Groups.TryGetValue("breakingChanges", out var breakingChangesMatch))
            return; // No breaking changes found.

        string breakingChanges = breakingChangesMatch.Value.Trim();

        // Only post if the breaking changes section contains "actual" content
        // (word characters or markdown links/images), not just punctuation.
        if (!MeaningfulContentRegex().IsMatch(breakingChanges))
            return;

        // Create a breaking changes topic.
        var topic = await discourseClient.CreateTopic(
            _discourseConfig.BreakingChangesCategoryId,
            StatusMessages.BreakingChangesTopicBody(request.PullRequest.Url, breakingChanges),
            request.PullRequest.Title,
            ct
        );

        var topicLink = _discourseConfig.Url + topic.PostUrl[1..];

        await client.AddComment(repository, prNumber, StatusMessages.BreakingChangesResponse(topicLink), ct);
    }

    private async Task OnReviewRequested(PullRequestEvent request, CancellationToken ct, GithubRepo repository)
    {
        if (await client.IsMaintainer(request.RequestedReviewer!.Login, repository, ct))
        {
            await labelManager.EnsureLabeled(request, StageOfWorkLabel.RequireReview, ct);
        }
    }

    private async Task OnLabelAdd(PullRequestEvent request, CancellationToken ct, string repoOwner, string repoName, int prNumber, GithubRepo repository)
    {
        if(!_discourseConfig.Enable)
            return;

        // ReSharper disable once NullableWarningSuppressionIsUsed
        if (request.Label?.Name != StatusLabel.UndergoingDiscussion)
            return;

        var exists = await topicsRepository.HasTopic(repoOwner, repoName, prNumber, ct);
        if (exists)
        {
            // need to make a new discussion.
            var topic = await discourseClient.CreateTopic(
                _discourseConfig.DiscussionCategoryId,
                StatusMessages.DiscourseTopicBody(request.PullRequest.Url),
                request.PullRequest.Title,
                ct
            );

            var topicLink = _discourseConfig.Url + topic.PostUrl[1..];

            await client.AddComment(repository, prNumber, StatusMessages.StartedDiscussionResponse(topicLink), ct);

            await discourseClient.ApplyTags(topic.TopicId, ct, _discourseConfig.Tagging.PrOpenTag);

            await topicsRepository.Add(repoOwner, repoName, prNumber, topic.TopicId, ct);
        }
    }

    private async Task OnOpened(PullRequestEvent request, CancellationToken ct, string?[] labels, PullRequest pr, GithubRepo repository)
    {
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

    private static bool ContainsLabelsStartingWith(IReadOnlyCollection<string?> labels, params string[] requiredStartedWith)
    {
        if (labels.Count == 0)
            return false;

        foreach (var startsWith in requiredStartedWith)
        {
            if (labels.All(x => x?.StartsWith(startsWith) != true))
                return false;
        }

        return true;
    }
}