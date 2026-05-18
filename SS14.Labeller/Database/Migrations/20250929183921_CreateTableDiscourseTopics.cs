using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SS14.Labeller.Database.Migrations
{
    /// <inheritdoc />
    public partial class CreateTableDiscourseTopics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "discourse");

            migrationBuilder.CreateTable(
                name: "discussions",
                schema: "discourse",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    repo_owner = table.Column<string>(type: "text", nullable: false),
                    repo_name = table.Column<string>(type: "text", nullable: false),
                    issue_number = table.Column<int>(type: "integer", nullable: false),
                    topic_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_discussions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_discussions_issue_number",
                schema: "discourse",
                table: "discussions",
                column: "issue_number");

            migrationBuilder.CreateIndex(
                name: "ix_discussions_repo_name",
                schema: "discourse",
                table: "discussions",
                column: "repo_name");

            migrationBuilder.CreateIndex(
                name: "ix_discussions_repo_owner",
                schema: "discourse",
                table: "discussions",
                column: "repo_owner");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "discussions",
                schema: "discourse");
        }
    }
}
