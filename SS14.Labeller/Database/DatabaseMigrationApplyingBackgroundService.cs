using Microsoft.Data.Sqlite;

namespace SS14.Labeller.Database;

public sealed class DatabaseMigrationApplyingBackgroundService(ILogger<DatabaseMigrationApplyingBackgroundService> logger, IConfiguration configuration) 
    : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connectionString = configuration.GetConnectionString("Default")
                               ?? "Data Source=Application.db";
        await using var con = new SqliteConnection(connectionString);

        Migrator.Migrate(con, "SS14.Labeller.Database.Migrations", logger);
    }
}