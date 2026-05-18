using Microsoft.EntityFrameworkCore;

namespace SS14.Labeller.Database;

/// <summary> Custom db-context that can be used in reusable way. Consumes all configurations that DI Container will pass.</summary>
public class CustomDbContext(DbContextOptions options, IEnumerable<IContextConfiguration> configurations)
    : DbContext(options)
{
    private readonly IEnumerable<IContextConfiguration> _configurations = configurations ?? throw new ArgumentNullException(nameof(configurations));

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        foreach (var config in _configurations)
        {
            config.Apply(modelBuilder);
        }
    }
}