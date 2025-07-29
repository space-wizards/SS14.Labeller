using Microsoft.Extensions.FileSystemGlobbing;
using SS14.Labeller.GitHubApi;
using SS14.Labeller.Labels;
using SS14.Labeller.Models;

namespace SS14.Labeller.Handlers;

public class LabelPullRequestHandler(IGitHubApiClient client) : RequestHandlerBase<PullRequestEvent>
{
    /// <inheritdoc />
    public override string EventType => "pull_request";

    /// <inheritdoc />
    protected override async Task HandleInternal(PullRequestEvent request, CancellationToken ct)
    {
        // I null-supress the shit out of these because i assume the github webhook json will basically never update and will always return valid data

        var pr = request.PullRequest;

        var number = pr.Number;
        var labels = pr.Labels
                       .Select(x => x.Name)
                       .ToArray();

        // basic labels
        var repository = request.Repository;

        if (request.Action == "opened")
        {
            if (labels.Length == 0)
                await client.AddLabel(repository, number, StatusLabels.Untriaged);

            var targetBranch = pr.Base.Ref;
            if (targetBranch == "stable" && !labels.Contains(BranchLabels.Stable))
                await client.AddLabel(repository, number, BranchLabels.Stable);
            else if (targetBranch == "staging" && !labels.Contains(BranchLabels.Staging))
                await client.AddLabel(repository, number, BranchLabels.Staging);

            var permission = await client.GetPermission(repository, pr.User.Login);
            if (permission is "write" or "admin")
                await client.AddLabel(repository, number, StatusLabels.Approved);
            else if (!labels.Contains(StatusLabels.RequireReview))
                await client.AddLabel(repository, number, StatusLabels.RequireReview);
        }

        if (request.Action is "synchronize" or "opened")
        {
            var totalDiff = pr.Additions + pr.Deletions;

            // remove the existing size/* labels
            foreach (var label in labels)
            {
                if (label?.StartsWith(SizeLabels.Prefix, StringComparison.OrdinalIgnoreCase) == true)
                {
                    await client.RemoveLabel(repository, number, label);
                }
            }

            var sizeLabel = SizeLabels.TryGetLabelFor(totalDiff);
            if (sizeLabel is not null && !labels.Contains(sizeLabel))
            {
                await client.AddLabel(repository, number, sizeLabel);
            }
        }

        var changedFiles = await client.GetChangedFiles(repository, number);

        var matcher = new Matcher();
        matcher.AddInclude("**/*.rsi/*.png");            // Sprites
        matcher.AddInclude("Resources/Maps/**/*.yml");   // Map
        matcher.AddInclude("Resources/Prototypes/Maps/**/*.yml");
        matcher.AddInclude("**/*.xaml*");                // UI
        matcher.AddInclude("**/*.swsl");                 // Shaders
        matcher.AddInclude("**/*.ogg");                  // Audio

        var sprites = new Matcher().AddInclude("**/*.rsi/*.png");
        var maps = new Matcher().AddInclude("Resources/Maps/**/*.yml")
                                .AddInclude("Resources/Prototypes/Maps/**/*.yml");
        var ui = new Matcher().AddInclude("**/*.xaml*");
        var shaders = new Matcher().AddInclude("**/*.swsl");
        var audio = new Matcher().AddInclude("**/*.ogg");
        var cs = new Matcher().AddInclude("**/*.cs");

        if (sprites.Match(changedFiles).HasMatches)
            await client.AddLabel(repository, number, ChangesLabels.Sprites);

        if (maps.Match(changedFiles).HasMatches)
            await client.AddLabel(repository, number, ChangesLabels.Map);

        if (ui.Match(changedFiles).HasMatches)
            await client.AddLabel(repository, number, ChangesLabels.Ui);

        if (shaders.Match(changedFiles).HasMatches)
            await client.AddLabel(repository, number, ChangesLabels.Shaders);

        if (audio.Match(changedFiles).HasMatches)
            await client.AddLabel(repository, number, ChangesLabels.Audio);

        if (!cs.Match(changedFiles).HasMatches)
            await client.AddLabel(repository, number, ChangesLabels.NoCSharp);
    }
}