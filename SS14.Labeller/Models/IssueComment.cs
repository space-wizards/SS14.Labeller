namespace SS14.Labeller.Models;

public class IssueComment
{
    public required string Body { get; set; }
    public required User User { get; set; }
}