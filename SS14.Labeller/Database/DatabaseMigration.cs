using FluentMigrator.Runner;

namespace SS14.Labeller.Database;

public sealed class DatabaseMigration
{
    public static void MigrateDatabase(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        // Put the database update into a scope to ensure
        // that all resources will be disposed.
        UpdateDatabase(scope.ServiceProvider);
    }

    /// <summary> Update the database </summary>
    private static void UpdateDatabase(IServiceProvider serviceProvider)
    {
        // Instantiate the runner
        var runner = serviceProvider.GetRequiredService<IMigrationRunner>();

        // Execute the migrations
        runner.MigrateUp();
    }
}
