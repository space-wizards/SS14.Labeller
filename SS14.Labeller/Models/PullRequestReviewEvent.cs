using System.Text.Json.Serialization;

namespace SS14.Labeller.Models;

public class PullRequestReviewEvent : EventBase
{
    [JsonPropertyName("pull_request")]
    public PullRequest PullRequest { get; set; }

    public Review Review { get; set; }
}

public class Review
{
    public User User { get; set; }
    public string State { get; set; }
}