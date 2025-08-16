using System.Data.Common;
using Dapper;

namespace SS14.Labeller.Repository.Queries;

public class FindTopicQuery : DatabaseQueryBase<int?>
{
    public required string RepoOwner { get; init; }

    public required string RepoName { get; init; }

    public required int IssueNumber { get; init; }

    public override async Task<int?> Query(DbConnection connection, CancellationToken ct)
    {
        var cd = GetCommand(ct);
        return await connection.QueryFirstOrDefaultAsync<int>(cd);
    }

    private const string Sql = $"""
                                SELECT TopicId FROM Discussions 
                                WHERE RepoOwner = @{nameof(RepoOwner)} AND RepoName = @{nameof(RepoName)} AND IssueNumber = @{nameof(IssueNumber)}
                                """;

    /// <inheritdoc />
    protected override string GetSql() => Sql;
}