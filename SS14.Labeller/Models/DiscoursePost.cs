using System.Text.Json.Serialization;

namespace SS14.Labeller.Models;

public class DiscoursePost
{
    [JsonPropertyName("post_url")]
    public required string PostUrl { get; set; }
}