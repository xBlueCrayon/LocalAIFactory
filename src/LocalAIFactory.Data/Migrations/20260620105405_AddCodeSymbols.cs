using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalAIFactory.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCodeSymbols : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CodeSymbols",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Uid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    SourceArtifactId = table.Column<int>(type: "int", nullable: false),
                    FileLocusKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourceLocusKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ParentSymbolId = table.Column<int>(type: "int", nullable: true),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Signature = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Access = table.Column<int>(type: "int", nullable: false),
                    IsPublic = table.Column<bool>(type: "bit", nullable: false),
                    StartOffset = table.Column<int>(type: "int", nullable: false),
                    EndOffset = table.Column<int>(type: "int", nullable: false),
                    StartLine = table.Column<int>(type: "int", nullable: false),
                    EndLine = table.Column<int>(type: "int", nullable: false),
                    ComplexitySignal = table.Column<int>(type: "int", nullable: false),
                    DetectedLanguage = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SymbolHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExtractedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeSymbols", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CodeSymbols_CodeSymbols_ParentSymbolId",
                        column: x => x.ParentSymbolId,
                        principalTable: "CodeSymbols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CodeSymbols_ImportedFiles_SourceArtifactId",
                        column: x => x.SourceArtifactId,
                        principalTable: "ImportedFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CodeSymbols_FileLocusKey",
                table: "CodeSymbols",
                column: "FileLocusKey");

            migrationBuilder.CreateIndex(
                name: "IX_CodeSymbols_ParentSymbolId",
                table: "CodeSymbols",
                column: "ParentSymbolId");

            migrationBuilder.CreateIndex(
                name: "IX_CodeSymbols_ProjectId_Kind",
                table: "CodeSymbols",
                columns: new[] { "ProjectId", "Kind" });

            migrationBuilder.CreateIndex(
                name: "IX_CodeSymbols_SourceArtifactId",
                table: "CodeSymbols",
                column: "SourceArtifactId");

            migrationBuilder.CreateIndex(
                name: "IX_CodeSymbols_SourceLocusKey",
                table: "CodeSymbols",
                column: "SourceLocusKey");

            migrationBuilder.CreateIndex(
                name: "IX_CodeSymbols_Uid",
                table: "CodeSymbols",
                column: "Uid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CodeSymbols");
        }
    }
}
