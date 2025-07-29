using SS14.Labeller.Models;

namespace SS14.Labeller.GitHubApi;

public interface IGitHubApiClient
{
    Task AddLabel(Repository repo, int number, string label, CancellationToken ct);
    Task RemoveLabel(Repository repo, int number, string label, CancellationToken ct);
    Task<List<string>> GetChangedFiles(Repository repo, int prNumber, CancellationToken ct);
    Task<string?> GetPermission(Repository repo, string? user, CancellationToken ct);
}