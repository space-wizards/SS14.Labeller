using MessagePipe;
using SS14.Labeller.GitHubApi;
using SS14.Labeller.Labelling;
using SS14.Labeller.Labelling.Labels;
using SS14.Labeller.Models;

namespace SS14.Labeller.Handlers;

public class LabelPullRequestReviewHandler(IGitHubApiClient client, ILabelManager labelManager)
    : IAsyncMessageHandler<PullRequestReviewEvent>
{
    /// <inheritdoc />
    public async ValueTask HandleAsync(PullRequestReviewEvent message, CancellationToken ct)
    {
        var pr = message.PullRequest;
        var repo = message.Repository;
        var user = message.Review.User.Login;

        // only process if the review state is "approved" or "changes_messageed" (ignore comments and other states)
        var state = message.Review.State;
        if (state != "approved" && state != "changes_messageed")
            return;

        // Ignore reviews if PR is closed or merged
        // "closed" means closed or merged, but let's also check for merged explicitly if available
        var isClosed = message.Review.State == "closed";
        var isMerged = pr.MergedAt != null;
        if (isClosed || isMerged)
            return;

        var isMaintainer = await client.IsMaintainer(user, repo, ct);
        if (isMaintainer)
        {
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
            await (state switch
            {
                "approved"
                    => labelManager.EnsureLabeled(message, StatusLabel.Approved, ct),
                "changes_messageed" 
                    => labelManager.EnsureLabeled(message, StageOfWorkLabel.AwaitingChanges, ct)
            });
#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
        }
    }
}