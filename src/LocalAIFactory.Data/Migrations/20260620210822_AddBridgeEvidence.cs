using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalAIFactory.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBridgeEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Confidence",
                table: "CodeSymbolReferences",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Evidence",
                table: "CodeSymbolReferences",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Evidence",
                table: "CodeEdges",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Confidence",
                table: "CodeSymbolReferences");

            migrationBuilder.DropColumn(
                name: "Evidence",
                table: "CodeSymbolReferences");

            migrationBuilder.DropColumn(
                name: "Evidence",
                table: "CodeEdges");
        }
    }
}
