using Microsoft.Extensions.Options;
using SS14.Labeller.Configuration;

namespace SS14.Labeller.GitHubApi;

/// <summary>
/// Provides a static GitHub personal access token for API authentication.
/// </summary>
public sealed class PatTokenProvider(IOptions<GitHubConfig> config) : IGitHubTokenProvider
{
    /// <inheritdoc />
    public Task<string> GetTokenAsync(string owner, string repo, CancellationToken ct)
    {
        return Task.FromResult(config.Value.Token);
    }

    /// <inheritdoc />
    public void InvalidateToken()
    {
        // PATs are static; nothing to invalidate.
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // noop
    }
}
