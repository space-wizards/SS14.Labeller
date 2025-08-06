using System.Text.Json.Serialization;

namespace SS14.Labeller.Models;

public class DiscoursePost
{
    [JsonPropertyName("id")]
    public required int TopicId { get; set; }

    [JsonPropertyName("title")]
    public required string Title { get; set; }
    [JsonPropertyName("category_id")]
    public required int CategoryId { get; set; }
}