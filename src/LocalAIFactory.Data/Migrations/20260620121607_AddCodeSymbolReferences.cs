using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalAIFactory.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCodeSymbolReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CodeSymbolReferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    FromSymbolId = table.Column<int>(type: "int", nullable: false),
                    SourceArtifactId = table.Column<int>(type: "int", nullable: false),
                    ReferenceKind = table.Column<int>(type: "int", nullable: false),
                    ReferencedDatabase = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    ReferencedSchema = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ReferencedObject = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ReferencedColumn = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ReferencedKey = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    FileLocusKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExtractedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeSymbolReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CodeSymbolReferences_CodeSymbols_FromSymbolId",
                        column: x => x.FromSymbolId,
                        principalTable: "CodeSymbols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CodeSymbolReferences_ImportedFiles_SourceArtifactId",
                        column: x => x.SourceArtifactId,
                        principalTable: "ImportedFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CodeSymbolReferences_FromSymbolId",
                table: "CodeSymbolReferences",
                column: "FromSymbolId");

            migrationBuilder.CreateIndex(
                name: "IX_CodeSymbolReferences_ProjectId_ReferenceKind",
                table: "CodeSymbolReferences",
                columns: new[] { "ProjectId", "ReferenceKind" });

            migrationBuilder.CreateIndex(
                name: "IX_CodeSymbolReferences_ReferencedKey",
                table: "CodeSymbolReferences",
                column: "ReferencedKey");

            migrationBuilder.CreateIndex(
                name: "IX_CodeSymbolReferences_SourceArtifactId",
                table: "CodeSymbolReferences",
                column: "SourceArtifactId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CodeSymbolReferences");
        }
    }
}
