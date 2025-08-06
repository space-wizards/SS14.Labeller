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