using System.Text.Json.Serialization;

namespace SS14.Labeller.Models;

public class DiscourseCreatedPost
{
    [JsonPropertyName("post_url")]
    public required string PostUrl { get; set; }

    [JsonPropertyName("topic_id")]
    public required int TopicId { get; set; }
}