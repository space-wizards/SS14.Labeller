using Dapper;
using System.Data.Common;

namespace SS14.Labeller.Repository.Commands;

public class InsertDiscourseTopicCommand : DatabaseCommandBase<int>
{
    public required string RepoOwner { get; init; }

    public required string RepoName { get; init; }

    public required int IssueNumber { get; init; }

    public required int TopicId { get; init; }

    public override async Task<int> Execute(DbConnection connection)
    {
        return await connection.ExecuteAsync(Sql, this);
    }

    private const string Sql = $"""
                                INSERT INTO Discussions (RepoOwner, RepoName, IssueNumber, TopicId)
                                VALUES (@{nameof(RepoOwner)}, @{nameof(RepoName)}, @{nameof(IssueNumber)}, @{nameof(TopicId)});
                                """;
}