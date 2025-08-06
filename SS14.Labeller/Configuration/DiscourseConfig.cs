using System.ComponentModel.DataAnnotations;

namespace SS14.Labeller.Configuration;

public class DiscourseConfig
{
    public const string Name = "Discourse";

    public bool Enable { get; set; } = false;

    [Required]
    public string ApiKey { get; set; } = string.Empty;
    [Required]
    public string Username { get; set; } = string.Empty;
    [Required]
    public int DiscussionCategoryId { get; set; }
    [Required]
    public string Url { get; set; } = string.Empty;

    public DiscourseTagConfig Tagging { get; set; } = new DiscourseTagConfig();
}

public class DiscourseTagConfig
{
    public string PrOpenTag { get; set; } = "pr-open";
    public string PrClosedTag { get; set; } = "pr-closed";
    public string PrMergedTag { get; set; } = "pr-merged";
}