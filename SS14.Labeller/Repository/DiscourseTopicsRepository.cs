using SS14.Labeller.Repository.Commands;
using SS14.Labeller.Repository.Queries;

namespace SS14.Labeller.Repository;

public class DiscourseTopicsRepository(IConfiguration configuration)
    : RepositoryBase(configuration), IDiscourseTopicsRepository
{
    public async Task<int?> FindTopicIdForDiscussion(string owner, string repoName, int issueNumber, CancellationToken ct)
    {
        await using var connection = OpenConnection();
        return await new FindTopicQuery
        {
            RepoOwner = owner,
            RepoName = repoName,
            IssueNumber = issueNumber
        }.Query(connection, ct);
    }

    public async Task<bool> HasTopic(string repoOwner, string repoName, int issueNumber, CancellationToken ct)
    {
        return (await FindTopicIdForDiscussion(repoOwner, repoName, issueNumber, ct)).HasValue;
    }

    public async Task Add(string owner, string name, int issueNumber, int topicId, CancellationToken ct)
    {
        await using var connection = OpenConnection();
        await new InsertDiscourseTopicCommand
        {
            IssueNumber = issueNumber,
            RepoName = name,
            RepoOwner = owner,
            TopicId = topicId
        }.Execute(connection, ct);
    }
}