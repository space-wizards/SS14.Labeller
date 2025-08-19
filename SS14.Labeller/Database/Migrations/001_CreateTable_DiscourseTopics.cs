using FluentMigrator;
using System.Diagnostics.CodeAnalysis;

namespace SS14.Labeller.Database.Migrations;

[Migration(20250818113000)]
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
public class CreateTableDiscourseTopics : Migration
{
    public const string TableName = "discussions";
    public const string SchemaName = "discourse";

    public override void Up()
    {
        Create.Schema(SchemaName);

        Create.Table(TableName)
              .InSchema(SchemaName)
              .WithColumn("id").AsInt32().PrimaryKey().Identity()
              .WithColumn("repo_owner").AsString().NotNullable()
              .WithColumn("repo_name").AsString().NotNullable()
              .WithColumn("issue_number").AsInt32().NotNullable()
              .WithColumn("topic_id").AsInt32().NotNullable();

        Create.Index("discussion_repo_owner_ix").OnTable(TableName).InSchema(SchemaName).OnColumn("repo_owner");
        Create.Index("discussion_issue_number_ix").OnTable(TableName).InSchema(SchemaName).OnColumn("issue_number");
        Create.Index("discussion_repo_name_ix").OnTable(TableName).InSchema(SchemaName).OnColumn("repo_name");
    }

    public override void Down()
    {
        // no-op
    }
}