using SS14.Labeller.GitHubApi;
using SS14.Labeller.Labelling.Labels;
using SS14.Labeller.Models;

namespace SS14.Labeller.Labelling;

public class LabelManager(IGitHubApiClient client) : ILabelManager
{
    public async Task EnsureLabeled(IPullRequestAwareEvent @event, LabelBase requestedLabel, CancellationToken ct)
    {
        var alreadyHaveLabel = false;
        foreach (var label in @event.PullRequest.Labels)
        {
            if (label.Name == null)
                continue;

            if (label.Name == requestedLabel)
            {
                alreadyHaveLabel = true;
                continue;
            }

            if (!requestedLabel.AllowMultiple && requestedLabel.TryGetFromString(label.Name, out var foundLabel))
            {
                await client.RemoveLabel(@event.Repository, @event.PullRequest.Number, foundLabel, ct);
            }
        }

        if (alreadyHaveLabel)
            return;

        await client.AddLabel(@event.Repository, @event.PullRequest.Number, requestedLabel, ct);
    }

    public async Task EnsureNotLabeled(IPullRequestAwareEvent @event, LabelBase requestedLabel, CancellationToken ct)
    {
        if (@event.PullRequest.Labels.Any(label => label.Name == requestedLabel))
        {
            await client.RemoveLabel(@event.Repository, @event.PullRequest.Number, requestedLabel, ct);
        }
    }
}