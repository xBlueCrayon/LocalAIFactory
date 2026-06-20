using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalAIFactory.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPermanenceTiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Kind",
                table: "PromptTemplates",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Tier",
                table: "ProjectProfileSections",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Tier",
                table: "KnowledgeRelationships",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Tier",
                table: "KnowledgeItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Tier",
                table: "KnowledgeEntities",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Tier",
                table: "BusinessRules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Tier",
                table: "ApprovedCodeSnippets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ProposedRevisions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TargetEntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TargetEntityId = table.Column<int>(type: "int", nullable: false),
                    OriginalKnowledgeItemId = table.Column<int>(type: "int", nullable: true),
                    ProposedTitle = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ProposedContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ChangeReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Source = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProposedRevisions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProposedRevisions_Status",
                table: "ProposedRevisions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ProposedRevisions_TargetEntityType_TargetEntityId",
                table: "ProposedRevisions",
                columns: new[] { "TargetEntityType", "TargetEntityId" });

            // KE-002 backfill (non-destructive: only sets the new Tier column). Tier 0 = Derived
            // (the AddColumn default), Tier 1 = Curated. Existing approved/human-anchored rows are
            // promoted to Curated; everything else stays Derived. Status = 1 is Approved.
            migrationBuilder.Sql("UPDATE [KnowledgeItems] SET [Tier] = 1 WHERE [Status] = 1 OR [IsApproved] = 1;");
            migrationBuilder.Sql("UPDATE [BusinessRules] SET [Tier] = 1 WHERE [Status] = 1 OR [IsApproved] = 1;");
            migrationBuilder.Sql("UPDATE [KnowledgeEntities] SET [Tier] = 1 WHERE [Status] = 1;");
            migrationBuilder.Sql("UPDATE [KnowledgeRelationships] SET [Tier] = 1 WHERE [Status] = 1;");
            migrationBuilder.Sql("UPDATE [ProjectProfileSections] SET [Tier] = 1 WHERE [Status] = 1;");
            migrationBuilder.Sql("UPDATE [ApprovedCodeSnippets] SET [Tier] = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProposedRevisions");

            migrationBuilder.DropColumn(
                name: "Tier",
                table: "ProjectProfileSections");

            migrationBuilder.DropColumn(
                name: "Tier",
                table: "KnowledgeRelationships");

            migrationBuilder.DropColumn(
                name: "Tier",
                table: "KnowledgeItems");

            migrationBuilder.DropColumn(
                name: "Tier",
                table: "KnowledgeEntities");

            migrationBuilder.DropColumn(
                name: "Tier",
                table: "BusinessRules");

            migrationBuilder.DropColumn(
                name: "Tier",
                table: "ApprovedCodeSnippets");

            migrationBuilder.AlterColumn<string>(
                name: "Kind",
                table: "PromptTemplates",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
