using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalAIFactory.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddImportCoverage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExtractionNote",
                table: "ImportedFiles",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ExtractionStatus",
                table: "ImportedFiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ImportCoverageReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Uid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    IngestionJobId = table.Column<int>(type: "int", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FilesDiscovered = table.Column<int>(type: "int", nullable: false),
                    FilesImported = table.Column<int>(type: "int", nullable: false),
                    FilesSkipped = table.Column<int>(type: "int", nullable: false),
                    FilesExtracted = table.Column<int>(type: "int", nullable: false),
                    FilesNoSymbols = table.Column<int>(type: "int", nullable: false),
                    FilesUnsupported = table.Column<int>(type: "int", nullable: false),
                    FilesParseError = table.Column<int>(type: "int", nullable: false),
                    FilesNonCode = table.Column<int>(type: "int", nullable: false),
                    SymbolCount = table.Column<int>(type: "int", nullable: false),
                    ReferenceCount = table.Column<int>(type: "int", nullable: false),
                    ResolvedReferences = table.Column<int>(type: "int", nullable: false),
                    UnresolvedReferences = table.Column<int>(type: "int", nullable: false),
                    EdgeCount = table.Column<int>(type: "int", nullable: false),
                    LanguageBreakdownJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SkipReasonsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParseErrorsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConfidenceJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UnsupportedLanguagesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProjectScopedOnly = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportCoverageReports", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ImportedFiles_ProjectId_ExtractionStatus",
                table: "ImportedFiles",
                columns: new[] { "ProjectId", "ExtractionStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportCoverageReports_ProjectId_CreatedUtc",
                table: "ImportCoverageReports",
                columns: new[] { "ProjectId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ImportCoverageReports_Uid",
                table: "ImportCoverageReports",
                column: "Uid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImportCoverageReports");

            migrationBuilder.DropIndex(
                name: "IX_ImportedFiles_ProjectId_ExtractionStatus",
                table: "ImportedFiles");

            migrationBuilder.DropColumn(
                name: "ExtractionNote",
                table: "ImportedFiles");

            migrationBuilder.DropColumn(
                name: "ExtractionStatus",
                table: "ImportedFiles");
        }
    }
}
