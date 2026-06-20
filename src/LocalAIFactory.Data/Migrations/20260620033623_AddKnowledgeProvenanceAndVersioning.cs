using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalAIFactory.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeProvenanceAndVersioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "Uid",
                table: "ProposedRevisions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "Uid",
                table: "ProjectProfileSections",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "Uid",
                table: "ProjectProfiles",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "Uid",
                table: "KnowledgeRelationships",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "Authority",
                table: "KnowledgeItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "KnowledgeItems",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveUtc",
                table: "KnowledgeItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryUtc",
                table: "KnowledgeItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "KnowledgeType",
                table: "KnowledgeItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "QualityBand",
                table: "KnowledgeItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Scope",
                table: "KnowledgeItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "KnowledgeItems",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "Uid",
                table: "KnowledgeItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "VersionNumber",
                table: "KnowledgeItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "Uid",
                table: "KnowledgeEntities",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "Uid",
                table: "BusinessRules",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "Uid",
                table: "ApprovedCodeSnippets",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "KnowledgeVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Uid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KnowledgeItemId = table.Column<int>(type: "int", nullable: false),
                    KnowledgeItemUid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    ContentSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ChangeReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Method = table.Column<int>(type: "int", nullable: false),
                    Actor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TierAtVersion = table.Column<int>(type: "int", nullable: false),
                    StatusAtVersion = table.Column<int>(type: "int", nullable: false),
                    PreviousVersionUid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeVersions_KnowledgeItems_KnowledgeItemId",
                        column: x => x.KnowledgeItemId,
                        principalTable: "KnowledgeItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProvenanceEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Uid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KnowledgeItemId = table.Column<int>(type: "int", nullable: false),
                    KnowledgeItemUid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceArtifactId = table.Column<int>(type: "int", nullable: true),
                    Method = table.Column<int>(type: "int", nullable: false),
                    ExtractorOrModelId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Actor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    OriginInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OriginPackUid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProvenanceEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProvenanceEvents_KnowledgeItems_KnowledgeItemId",
                        column: x => x.KnowledgeItemId,
                        principalTable: "KnowledgeItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // KE-003: assign a unique Uid to every pre-existing row BEFORE the unique indexes are created.
            // Existing rows default to Guid.Empty, which would violate the unique index. These are local
            // rows that predate any export, so NEWID() is sufficient; new rows use app-side Guid v7.
            migrationBuilder.Sql("UPDATE [KnowledgeItems] SET [Uid] = NEWID() WHERE [Uid] = '00000000-0000-0000-0000-000000000000';");
            migrationBuilder.Sql("UPDATE [BusinessRules] SET [Uid] = NEWID() WHERE [Uid] = '00000000-0000-0000-0000-000000000000';");
            migrationBuilder.Sql("UPDATE [ApprovedCodeSnippets] SET [Uid] = NEWID() WHERE [Uid] = '00000000-0000-0000-0000-000000000000';");
            migrationBuilder.Sql("UPDATE [ProjectProfiles] SET [Uid] = NEWID() WHERE [Uid] = '00000000-0000-0000-0000-000000000000';");
            migrationBuilder.Sql("UPDATE [ProjectProfileSections] SET [Uid] = NEWID() WHERE [Uid] = '00000000-0000-0000-0000-000000000000';");
            migrationBuilder.Sql("UPDATE [KnowledgeEntities] SET [Uid] = NEWID() WHERE [Uid] = '00000000-0000-0000-0000-000000000000';");
            migrationBuilder.Sql("UPDATE [KnowledgeRelationships] SET [Uid] = NEWID() WHERE [Uid] = '00000000-0000-0000-0000-000000000000';");
            migrationBuilder.Sql("UPDATE [ProposedRevisions] SET [Uid] = NEWID() WHERE [Uid] = '00000000-0000-0000-0000-000000000000';");

            migrationBuilder.CreateIndex(
                name: "IX_ProposedRevisions_Uid",
                table: "ProposedRevisions",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectProfileSections_Uid",
                table: "ProjectProfileSections",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectProfiles_Uid",
                table: "ProjectProfiles",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeRelationships_Uid",
                table: "KnowledgeRelationships",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeItems_Uid",
                table: "KnowledgeItems",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeEntities_Uid",
                table: "KnowledgeEntities",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessRules_Uid",
                table: "BusinessRules",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovedCodeSnippets_Uid",
                table: "ApprovedCodeSnippets",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeVersions_KnowledgeItemId_VersionNumber",
                table: "KnowledgeVersions",
                columns: new[] { "KnowledgeItemId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeVersions_Uid",
                table: "KnowledgeVersions",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProvenanceEvents_KnowledgeItemId_CreatedUtc",
                table: "ProvenanceEvents",
                columns: new[] { "KnowledgeItemId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ProvenanceEvents_OriginPackUid",
                table: "ProvenanceEvents",
                column: "OriginPackUid");

            migrationBuilder.CreateIndex(
                name: "IX_ProvenanceEvents_Uid",
                table: "ProvenanceEvents",
                column: "Uid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KnowledgeVersions");

            migrationBuilder.DropTable(
                name: "ProvenanceEvents");

            migrationBuilder.DropIndex(
                name: "IX_ProposedRevisions_Uid",
                table: "ProposedRevisions");

            migrationBuilder.DropIndex(
                name: "IX_ProjectProfileSections_Uid",
                table: "ProjectProfileSections");

            migrationBuilder.DropIndex(
                name: "IX_ProjectProfiles_Uid",
                table: "ProjectProfiles");

            migrationBuilder.DropIndex(
                name: "IX_KnowledgeRelationships_Uid",
                table: "KnowledgeRelationships");

            migrationBuilder.DropIndex(
                name: "IX_KnowledgeItems_Uid",
                table: "KnowledgeItems");

            migrationBuilder.DropIndex(
                name: "IX_KnowledgeEntities_Uid",
                table: "KnowledgeEntities");

            migrationBuilder.DropIndex(
                name: "IX_BusinessRules_Uid",
                table: "BusinessRules");

            migrationBuilder.DropIndex(
                name: "IX_ApprovedCodeSnippets_Uid",
                table: "ApprovedCodeSnippets");

            migrationBuilder.DropColumn(
                name: "Uid",
                table: "ProposedRevisions");

            migrationBuilder.DropColumn(
                name: "Uid",
                table: "ProjectProfileSections");

            migrationBuilder.DropColumn(
                name: "Uid",
                table: "ProjectProfiles");

            migrationBuilder.DropColumn(
                name: "Uid",
                table: "KnowledgeRelationships");

            migrationBuilder.DropColumn(
                name: "Authority",
                table: "KnowledgeItems");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "KnowledgeItems");

            migrationBuilder.DropColumn(
                name: "EffectiveUtc",
                table: "KnowledgeItems");

            migrationBuilder.DropColumn(
                name: "ExpiryUtc",
                table: "KnowledgeItems");

            migrationBuilder.DropColumn(
                name: "KnowledgeType",
                table: "KnowledgeItems");

            migrationBuilder.DropColumn(
                name: "QualityBand",
                table: "KnowledgeItems");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "KnowledgeItems");

            migrationBuilder.DropColumn(
                name: "Summary",
                table: "KnowledgeItems");

            migrationBuilder.DropColumn(
                name: "Uid",
                table: "KnowledgeItems");

            migrationBuilder.DropColumn(
                name: "VersionNumber",
                table: "KnowledgeItems");

            migrationBuilder.DropColumn(
                name: "Uid",
                table: "KnowledgeEntities");

            migrationBuilder.DropColumn(
                name: "Uid",
                table: "BusinessRules");

            migrationBuilder.DropColumn(
                name: "Uid",
                table: "ApprovedCodeSnippets");
        }
    }
}
