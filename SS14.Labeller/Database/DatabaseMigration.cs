using Microsoft.EntityFrameworkCore;

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
        var contextFactory = serviceProvider.GetRequiredService<IDbContextFactory<CustomDbContext>>();
        using var context = contextFactory.CreateDbContext();
        var db = context.Database;

        db.EnsureCreated();

        db.Migrate();
    }
}
