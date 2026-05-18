namespace SS14.Labeller.Database.Entities;

public class DiscourseTopicEntity : EntityBase
{
    public required string RepoOwner { get; set; }

    public required string RepoName{ get; set; }

    public required int IssueNumber{ get; set; }

    public required int TopicId { get; set; }
}