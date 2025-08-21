using SS14.Labeller.GitHubApi;
using SS14.Labeller.Labelling;
using SS14.Labeller.Labelling.Labels;
using SS14.Labeller.Models;

namespace SS14.Labeller.Handlers;

public class LabelPullRequestReviewHandler(IGitHubApiClient client, ILabelManager labelManager)
    : RequestHandlerBase<PullRequestReviewEvent>
{
    /// <inheritdoc />
    protected override async Task HandleInternal(PullRequestReviewEvent request, CancellationToken ct)
    {
        var pr = request.PullRequest;
        var repo = request.Repository;
        var user = request.Review.User.Login;

        // only process if the review state is "approved" or "changes_requested" (ignore comments and other states)
        var state = request.Review.State;
        if (state != "approved" && state != "changes_requested")
            return;

        // Ignore reviews if PR is closed or merged
        // "closed" means closed or merged, but let's also check for merged explicitly if available
        var isClosed = request.Review.State == "closed";
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
                    => labelManager.EnsureLabeled(request, StatusLabel.Approved, ct),
                "changes_requested" 
                    => labelManager.EnsureLabeled(request, StageOfWorkLabel.AwaitingChanges, ct)
            });
#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
        }
    }
}