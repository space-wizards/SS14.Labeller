using SS14.Labeller.Models;

namespace SS14.Labeller.GitHubApi;

public interface IGitHubApiClient
{
    Task AddLabel(Repository repo, int number, string label);
    Task RemoveLabel(Repository repo, int number, string label);
    Task<List<string>> GetChangedFiles(Repository repo, int prNumber);
    Task<string?> GetPermission(Repository repo, string? user);
}