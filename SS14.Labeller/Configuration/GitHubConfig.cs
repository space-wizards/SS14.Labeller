using System.ComponentModel.DataAnnotations;

namespace SS14.Labeller.Configuration;

public class GitHubConfig
{
    public const string Name = "GitHub";

    [Required]
    public string WebhookSecret { get; set; } = string.Empty;
    [Required]
    public string Token { get; set; } = string.Empty;

    public int MaxRetryAttempt { get; set; } = 5;
}