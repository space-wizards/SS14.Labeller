namespace SS14.Labeller.GitHubApi;

/// <summary>
/// Provider for GitHub API tokens.
/// </summary>
public interface IGitHubTokenProvider : IDisposable
{
    /// <summary>
    /// Gets token for requesting GitHub API.
    /// </summary>
    Task<string> GetTokenAsync(string owner, string repo, CancellationToken ct);

    /// <summary>
    /// Invalidates any cached token so the next call to <see cref="GetTokenAsync"/> fetches a fresh one.
    /// </summary>
    void InvalidateToken();
}
