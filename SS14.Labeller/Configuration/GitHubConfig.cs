using System.ComponentModel.DataAnnotations;

namespace SS14.Labeller.Configuration;

public class GitHubConfig
{
    public const string Name = "GitHub";

    [Required]
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>
    /// The authentication mode used to talk to the GitHub API.
    /// </summary>
    public GitHubAuthMode AuthMode { get; set; }

    /// <summary>
    /// A GitHub personal access token. Required when <see cref="AuthMode"/> is <see cref="GitHubAuthMode.Pat"/>.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// The GitHub repository owner (user or organization) the app operates on.
    /// </summary>
    public required string Owner { get; set; }

    /// <summary>
    /// The GitHub repository name the app operates on.
    /// </summary>
    public required string Repo { get; set; } 

    /// <summary>
    /// The GitHub App ID. Required when <see cref="AuthMode"/> is <see cref="GitHubAuthMode.App"/>.
    /// </summary>
    public long AppId { get; set; }

    /// <summary>
    /// The PEM-encoded private key of the GitHub App. Required when <see cref="AuthMode"/> is <see cref="GitHubAuthMode.App"/>.
    /// </summary>
    public string AppPrivateKey { get; set; } = string.Empty;

    /// <summary>
    /// Max retry attempts on GitHub calls that are failing.
    /// </summary>
    public int MaxRetryAttempt { get; set; } = 5;

    /// <summary>
    /// GitHub API base url.
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.github.com";
}
