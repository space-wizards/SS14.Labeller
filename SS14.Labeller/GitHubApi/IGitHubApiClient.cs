using SS14.Labeller.Labelling.Labels;
using SS14.Labeller.Models;

namespace SS14.Labeller.GitHubApi;

public interface IGitHubApiClient
{
    Task AddLabel(GithubRepo repo, int number, LabelBase label, CancellationToken ct);
    Task RemoveLabel(GithubRepo repo, int number, LabelBase label, CancellationToken ct);
    Task<List<string>> GetChangedFiles(GithubRepo repo, int prNumber, CancellationToken ct);
    Task<bool> IsMaintainer(string? user, GithubRepo forRepository, CancellationToken ct);
    Task AddComment(GithubRepo repo, int number, string comment, CancellationToken ct);
    Task<List<IssueComment>> GetComments(GithubRepo repo, int prNumber, CancellationToken ct);
}