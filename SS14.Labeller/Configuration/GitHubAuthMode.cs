namespace SS14.Labeller.Configuration;

/// <summary>
/// Mode of authentication for GitHub calls.
/// </summary>
public enum GitHubAuthMode
{
    None = 0,

    /// <summary>
    /// Authenticate to the GitHub API with a personal access token (PAT).
    /// </summary>
    Pat = 1,

    /// <summary>
    /// Authenticate to the GitHub API as a GitHub App installation.
    /// </summary>
    App = 2,
}