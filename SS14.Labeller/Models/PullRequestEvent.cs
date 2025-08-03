using System.Text.Json.Serialization;

namespace SS14.Labeller.Models;

public class PullRequestEvent : EventBase
{
    [JsonPropertyName("pull_request")]
    public required PullRequest PullRequest { get; set; }
}

public class PullRequest
{
    public int Number { get; set; }

    public required Label[] Labels { get; set; }

    public required User User { get; set; }

    public required BranchInfo Base { get; set; }

    public int Additions { get; set; }

    public int Deletions { get; set; }

    [JsonPropertyName("merged_at")]
    public string? MergedAt { get; set; }
}

public class BranchInfo
{
    public required string Ref { get; set; }
}