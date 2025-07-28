using System.Text.Json.Serialization;

namespace SS14.Labeller.Models;

public class PullRequestEvent : EventBase
{
    [JsonPropertyName("pull_request")]
    public PullRequest PullRequest { get; set; }
}

public class PullRequest
{
    public int Number { get; set; }
    public Label[] Labels { get; set; }
    public User User { get; set; }
    public BranchInfo Base { get; set; }
    public int Additions { get; set; }
    public int Deletions { get; set; }
    [JsonPropertyName("merged_at")]
    public string? MergedAt { get; set; }
}

public class BranchInfo
{
    public string Ref { get; set; }

}