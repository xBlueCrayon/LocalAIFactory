using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalAIFactory.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgePack : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "KnowledgePackId",
                table: "KnowledgeItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastReviewedUtc",
                table: "KnowledgeItems",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KnowledgePacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Uid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    License = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    InstalledUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ItemCount = table.Column<int>(type: "int", nullable: false),
                    SourceManifestHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgePacks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeItems_KnowledgePackId",
                table: "KnowledgeItems",
                column: "KnowledgePackId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgePacks_Name",
                table: "KnowledgePacks",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgePacks_Uid",
                table: "KnowledgePacks",
                column: "Uid",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_KnowledgeItems_KnowledgePacks_KnowledgePackId",
                table: "KnowledgeItems",
                column: "KnowledgePackId",
                principalTable: "KnowledgePacks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_KnowledgeItems_KnowledgePacks_KnowledgePackId",
                table: "KnowledgeItems");

            migrationBuilder.DropTable(
                name: "KnowledgePacks");

            migrationBuilder.DropIndex(
                name: "IX_KnowledgeItems_KnowledgePackId",
                table: "KnowledgeItems");

            migrationBuilder.DropColumn(
                name: "KnowledgePackId",
                table: "KnowledgeItems");

            migrationBuilder.DropColumn(
                name: "LastReviewedUtc",
                table: "KnowledgeItems");
        }
    }
}
