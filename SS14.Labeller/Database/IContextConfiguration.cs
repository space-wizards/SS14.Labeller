using Microsoft.EntityFrameworkCore;

namespace SS14.Labeller.Database;

/// <summary>
/// Marker for types that can configure DbContext. Usually application will be one main DbContext,
/// for which every available <see cref="IContextConfiguration"/> implementation will be executed.
/// Interface that will help to apply all ef model configuration without interacting with one certain db-context.
/// </summary>
public interface IContextConfiguration
{
    /// <summary> Applies configuration to model.  </summary>
    void Apply(ModelBuilder modelBuilder);
}