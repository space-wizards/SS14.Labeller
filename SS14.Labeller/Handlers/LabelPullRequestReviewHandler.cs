using SS14.Labeller.GitHubApi;
using SS14.Labeller.Labels;
using SS14.Labeller.Models;

namespace SS14.Labeller.Handlers;

public class LabelPullRequestReviewHandler(IGitHubApiClient client)
    : RequestHandlerBase<PullRequestReviewEvent>
{
    /// <inheritdoc />
    public override string EventType => "pull_request_review";

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

        var number = pr.Number;
        var permission = await client.GetPermission(repo, user, ct);
        if (permission is "write" or "admin")
        {
#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
            await (state switch
            {
                "approved"
                    => client.AddLabel(repo, number, StatusLabels.Approved, ct),
                "changes_requested" =>
                    Task.WhenAll(
                        // We remove the Needs Review label, later down the line when a review is re-requested, we will apply this label again.
                        client.RemoveLabel(repo, number, StatusLabels.RequireReview, ct),
                        client.AddLabel(repo, number, StatusLabels.AwaitingChanges, ct)
                        )
            });
#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).

        }
    }
}