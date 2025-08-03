using SS14.Labeller.GitHubApi;
using SS14.Labeller.Labels;
using SS14.Labeller.Models;

namespace SS14.Labeller.Handlers;

public class LabelIssueHandler(IGitHubApiClient client) : RequestHandlerBase<IssuesEvent>
{
    /// <inheritdoc />
    public override string EventType => "issues";

    /// <inheritdoc />
    protected override async Task HandleInternal(IssuesEvent request, CancellationToken ct)
    {
        var action = request.Action;
        if (action == "opened")
        {
            var number = request.Issue.Number;
            var labels = request.Issue.Labels;

            if (labels.Length == 0)
                await client.AddLabel(request.Repository, number, StatusLabels.Untriaged, ct);
        }
    }
}