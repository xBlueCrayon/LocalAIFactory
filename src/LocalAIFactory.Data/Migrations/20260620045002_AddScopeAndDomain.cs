using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalAIFactory.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScopeAndDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KnowledgeDomainId",
                table: "KnowledgeItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KnowledgeDomains",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Uid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeDomains", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScopeApplicabilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Uid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConstraintKnowledgeItemId = table.Column<int>(type: "int", nullable: false),
                    ConstraintUid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetKind = table.Column<int>(type: "int", nullable: false),
                    TargetId = table.Column<int>(type: "int", nullable: false),
                    TargetUid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScopeApplicabilities", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeItems_KnowledgeDomainId",
                table: "KnowledgeItems",
                column: "KnowledgeDomainId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeItems_Scope",
                table: "KnowledgeItems",
                column: "Scope");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDomains_Code",
                table: "KnowledgeDomains",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDomains_Uid",
                table: "KnowledgeDomains",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScopeApplicabilities_ConstraintKnowledgeItemId",
                table: "ScopeApplicabilities",
                column: "ConstraintKnowledgeItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ScopeApplicabilities_TargetKind_TargetId",
                table: "ScopeApplicabilities",
                columns: new[] { "TargetKind", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_ScopeApplicabilities_Uid",
                table: "ScopeApplicabilities",
                column: "Uid",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_KnowledgeItems_KnowledgeDomains_KnowledgeDomainId",
                table: "KnowledgeItems",
                column: "KnowledgeDomainId",
                principalTable: "KnowledgeDomains",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // KE-005 backfill (non-destructive: only sets the Scope that was inert/Unspecified before).
            // KnowledgeScope: Global = 1, Project = 2. Items tied to a project get Project scope; the rest Global.
            migrationBuilder.Sql("UPDATE [KnowledgeItems] SET [Scope] = CASE WHEN [ProjectId] IS NOT NULL THEN 2 ELSE 1 END WHERE [Scope] = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KnowledgeItems_KnowledgeDomains_KnowledgeDomainId",
                table: "KnowledgeItems");

            migrationBuilder.DropTable(
                name: "KnowledgeDomains");

            migrationBuilder.DropTable(
                name: "ScopeApplicabilities");

            migrationBuilder.DropIndex(
                name: "IX_KnowledgeItems_KnowledgeDomainId",
                table: "KnowledgeItems");

            migrationBuilder.DropIndex(
                name: "IX_KnowledgeItems_Scope",
                table: "KnowledgeItems");

            migrationBuilder.DropColumn(
                name: "KnowledgeDomainId",
                table: "KnowledgeItems");
        }
    }
}
