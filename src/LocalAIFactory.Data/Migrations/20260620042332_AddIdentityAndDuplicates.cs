using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalAIFactory.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityAndDuplicates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceLocusKey",
                table: "KnowledgeItems",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SupersedesImportedFileId",
                table: "ImportedFiles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KnowledgeDuplicates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Uid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KnowledgeItemId = table.Column<int>(type: "int", nullable: false),
                    KnowledgeItemUid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DuplicateOfKnowledgeItemId = table.Column<int>(type: "int", nullable: false),
                    DuplicateOfUid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MatchKind = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Confidence = table.Column<double>(type: "float", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeDuplicates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeItems_SourceLocusKey",
                table: "KnowledgeItems",
                column: "SourceLocusKey");

            migrationBuilder.CreateIndex(
                name: "IX_ImportedFiles_SupersedesImportedFileId",
                table: "ImportedFiles",
                column: "SupersedesImportedFileId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDuplicates_DuplicateOfKnowledgeItemId",
                table: "KnowledgeDuplicates",
                column: "DuplicateOfKnowledgeItemId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDuplicates_KnowledgeItemId",
                table: "KnowledgeDuplicates",
                column: "KnowledgeItemId");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDuplicates_Status",
                table: "KnowledgeDuplicates",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDuplicates_Uid",
                table: "KnowledgeDuplicates",
                column: "Uid",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ImportedFiles_ImportedFiles_SupersedesImportedFileId",
                table: "ImportedFiles",
                column: "SupersedesImportedFileId",
                principalTable: "ImportedFiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImportedFiles_ImportedFiles_SupersedesImportedFileId",
                table: "ImportedFiles");

            migrationBuilder.DropTable(
                name: "KnowledgeDuplicates");

            migrationBuilder.DropIndex(
                name: "IX_KnowledgeItems_SourceLocusKey",
                table: "KnowledgeItems");

            migrationBuilder.DropIndex(
                name: "IX_ImportedFiles_SupersedesImportedFileId",
                table: "ImportedFiles");

            migrationBuilder.DropColumn(
                name: "SourceLocusKey",
                table: "KnowledgeItems");

            migrationBuilder.DropColumn(
                name: "SupersedesImportedFileId",
                table: "ImportedFiles");
        }
    }
}
