using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalAIFactory.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCodeEdgesAndGraph : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalizedKey",
                table: "CodeSymbols",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");

            // KE-010: backfill the object key for symbols extracted before this column existed (KE-008/009).
            migrationBuilder.Sql("UPDATE [CodeSymbols] SET [NormalizedKey] = LOWER([FullName]);");

            migrationBuilder.CreateTable(
                name: "CodeEdges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Uid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    FromSymbolId = table.Column<int>(type: "int", nullable: false),
                    ToSymbolId = table.Column<int>(type: "int", nullable: false),
                    RelationType = table.Column<int>(type: "int", nullable: false),
                    Confidence = table.Column<double>(type: "float", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Tier = table.Column<int>(type: "int", nullable: false),
                    SourceArtifactId = table.Column<int>(type: "int", nullable: false),
                    EdgeKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeEdges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CodeEdges_CodeSymbols_FromSymbolId",
                        column: x => x.FromSymbolId,
                        principalTable: "CodeSymbols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CodeEdges_CodeSymbols_ToSymbolId",
                        column: x => x.ToSymbolId,
                        principalTable: "CodeSymbols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CodeEdges_ImportedFiles_SourceArtifactId",
                        column: x => x.SourceArtifactId,
                        principalTable: "ImportedFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CodeSymbols_ProjectId_NormalizedKey",
                table: "CodeSymbols",
                columns: new[] { "ProjectId", "NormalizedKey" });

            migrationBuilder.CreateIndex(
                name: "IX_CodeEdges_EdgeKey",
                table: "CodeEdges",
                column: "EdgeKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CodeEdges_FromSymbolId",
                table: "CodeEdges",
                column: "FromSymbolId");

            migrationBuilder.CreateIndex(
                name: "IX_CodeEdges_ProjectId_RelationType",
                table: "CodeEdges",
                columns: new[] { "ProjectId", "RelationType" });

            migrationBuilder.CreateIndex(
                name: "IX_CodeEdges_SourceArtifactId",
                table: "CodeEdges",
                column: "SourceArtifactId");

            migrationBuilder.CreateIndex(
                name: "IX_CodeEdges_ToSymbolId",
                table: "CodeEdges",
                column: "ToSymbolId");

            migrationBuilder.CreateIndex(
                name: "IX_CodeEdges_Uid",
                table: "CodeEdges",
                column: "Uid",
                unique: true);

            // KE-010: the unified structural graph view — containment (derived from ParentSymbolId) UNION
            // reference edges (CodeEdges). RelationType 9 = PartOf. KE-011 traverses this single shape.
            migrationBuilder.Sql(@"
CREATE VIEW [vCodeGraph] AS
    SELECT s.[ProjectId], s.[Id] AS [FromSymbolId], s.[ParentSymbolId] AS [ToSymbolId],
           9 AS [RelationType], CAST(1.0 AS float) AS [Confidence], 'containment' AS [EdgeSource]
    FROM [CodeSymbols] s
    WHERE s.[ParentSymbolId] IS NOT NULL
    UNION ALL
    SELECT e.[ProjectId], e.[FromSymbolId], e.[ToSymbolId],
           e.[RelationType], e.[Confidence], 'reference' AS [EdgeSource]
    FROM [CodeEdges] e;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF OBJECT_ID('vCodeGraph', 'V') IS NOT NULL DROP VIEW [vCodeGraph];");

            migrationBuilder.DropTable(
                name: "CodeEdges");

            migrationBuilder.DropIndex(
                name: "IX_CodeSymbols_ProjectId_NormalizedKey",
                table: "CodeSymbols");

            migrationBuilder.DropColumn(
                name: "NormalizedKey",
                table: "CodeSymbols");
        }
    }
}
