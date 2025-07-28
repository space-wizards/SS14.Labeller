using System.Text.Json.Serialization;

namespace SS14.Labeller.Models;

public class PullRequestReviewEvent : EventBase
{
    [JsonPropertyName("pull_request")]
    public required PullRequest PullRequest { get; set; }

    public required Review Review { get; set; }
}

public class Review
{
    public required User User { get; set; }
    public required string State { get; set; }
}