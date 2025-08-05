using System.ComponentModel.DataAnnotations;

namespace SS14.Labeller.Configuration;

public class DiscourseConfig
{
    public const string Name = "Discourse";

    [Required]
    public string ApiKey { get; set; } = string.Empty;
    [Required]
    public string Username { get; set; } = string.Empty;
    [Required]
    public int DiscussionCategoryId { get; set; }
    [Required]
    public string Url { get; set; } = string.Empty;
}