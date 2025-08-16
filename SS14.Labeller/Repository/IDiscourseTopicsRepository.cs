namespace SS14.Labeller.Repository;

public interface IDiscourseTopicsRepository
{
    Task<int?> FindTopicIdForDiscussion(string owner, string repoName, int issueNumber, CancellationToken ct);

    Task<bool> HasTopic(string repoOwner, string repoName, int issueNumber, CancellationToken ct);

    Task Add(string owner, string name, int issueNumber, int topicId, CancellationToken ct);
}