using MessagePipe;
using SS14.Labeller.GitHubApi;
using SS14.Labeller.Labelling.Labels;
using SS14.Labeller.Models;

namespace SS14.Labeller.Handlers;

public class LabelIssueHandler(IGitHubApiClient client) : IAsyncMessageHandler<IssuesEvent>
{
    /// <inheritdoc />
    public async ValueTask HandleAsync(IssuesEvent message, CancellationToken ct)
    {
        var action = message.Action;
        if (action == "opened")
        {
            var number = message.Issue.Number;
            var labels = message.Issue.Labels;

            if (labels.Length == 0)
                await client.AddLabel(message.Repository, number, StatusLabel.Untriaged, ct);
        }
    }
}