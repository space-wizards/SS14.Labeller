using Dapper;
using Microsoft.Data.Sqlite;

namespace SS14.Labeller.Database;

public sealed class DataManager(ILogger<DataManager> logger) : IHostedService
{
    public SqliteConnection OpenConnection()
    {
        var con = new SqliteConnection(GetConnectionString());
        con.Open();
        return con;
    }

    public async Task<int?> GetTopicIdForDiscussion(string owner, string repoName, int issueNumber)
    {
        await using var connection = OpenConnection();
        const string sql = """
                               SELECT TopicId FROM Discussions 
                               WHERE RepoOwner = @Owner AND RepoName = @Name AND IssueNumber = @Number
                           """;

        var value = await connection.QuerySingleAsync<int?>(sql, new
        {
            Owner = owner,
            Name = repoName,
            Number = issueNumber
        });

        return value;
    }

    private string GetConnectionString()
    {
        return "Data Source=Application.db";
    }

    Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        var con = OpenConnection();

        Migrator.Migrate(con, "SS14.Labeller.Database.Migrations", logger);
        return Task.CompletedTask;
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken)
    {
        // No shutdown needed.
        return Task.CompletedTask;
    }
}