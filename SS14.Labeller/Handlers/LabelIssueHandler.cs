using Microsoft.Extensions.Options;
using SS14.Labeller.Configuration;
using SS14.Labeller.GitHubApi;
using SS14.Labeller.Labelling.Labels;
using SS14.Labeller.Models;

namespace SS14.Labeller.Handlers;

public class LabelIssueHandler(IGitHubApiClient client, IOptions<LabellerConfig> options) : RequestHandlerBase<IssuesEvent>
{
    /// <inheritdoc />
    protected override async Task HandleInternal(IssuesEvent request, CancellationToken ct)
    {
        var action = request.Action;
        if (!options.Value.LabelIssuesOnRepositories.Contains(request.Repository.Name))
        {
            await HandleOtherRepositoryIssue(action, request, ct);
            return;
        }

        if (action == "opened")
        {
            var number = request.Issue.Number;
            var labels = request.Issue.Labels;

            if (labels.Length == 0)
                await client.AddLabel(request.Repository, number, StatusLabel.Untriaged, ct);
        }
    }

    private async Task HandleOtherRepositoryIssue(string action, IssuesEvent request, CancellationToken ct)
    {
        if (action != "labeled")
            return;

        if (request.Issue.Labels.Any(x => x.Name == "IsValid"))
        {
            var targetRepo = new GithubRepo
            {
                Name = options.Value.ForwardIssuesToRepository,
                Owner = request.Repository.Owner
            };
            var linkToCreated = await client.CreateIssue(request.Issue.Title, request.Issue.Body, targetRepo, ct);
            await client.AddComment(
                request.Repository,
                request.Issue.Number,
                $"As issue was marked as valid, automation created copy of this issue in main repo - {linkToCreated}",
                ct
            );
        }
    }
}