using Dapper;
using System.Data.Common;

namespace SS14.Labeller.Repository.Commands;

public class InsertDiscourseTopicCommand : DatabaseCommandBase<int>
{
    public required string RepoOwner { get; init; }

    public required string RepoName { get; init; }

    public required int IssueNumber { get; init; }

    public required int TopicId { get; init; }

    public override async Task<int> Execute(DbConnection connection, CancellationToken ct)
    {
        var cd = GetCommand(ct);
        return await connection.ExecuteAsync(cd);
    }

    private const string Sql = $"""
                                INSERT INTO discourse.discussions (repo_owner, repo_name, issue_number, topic_id)
                                VALUES (@{nameof(RepoOwner)}, @{nameof(RepoName)}, @{nameof(IssueNumber)}, @{nameof(TopicId)});
                                """;

    /// <inheritdoc />
    protected override string GetSql() => Sql;
}