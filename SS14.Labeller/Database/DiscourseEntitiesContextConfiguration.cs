using Microsoft.EntityFrameworkCore;
using SS14.Labeller.Database.Entities;

namespace SS14.Labeller.Database;

public class DiscourseEntitiesContextConfiguration : IContextConfiguration
{
    public const string TableName = "discussions";

    public const string SchemaName = "discourse";
    
    /// <inheritdoc />
    public void Apply(ModelBuilder modelBuilder)
    {
        var ent = modelBuilder.Entity<DiscourseTopicEntity>()
                              .ToTable(TableName, SchemaName);
        ent.HasIndex(x => x.RepoOwner);
        ent.HasIndex(x => x.RepoName);
        ent.HasIndex(x => x.IssueNumber);
    }
}