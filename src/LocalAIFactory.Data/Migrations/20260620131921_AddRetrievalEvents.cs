using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalAIFactory.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRetrievalEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RetrievalEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Uid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<int>(type: "int", nullable: true),
                    Query = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Mode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ResultCount = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetrievalEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RetrievalEvents_ProjectId_CreatedUtc",
                table: "RetrievalEvents",
                columns: new[] { "ProjectId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RetrievalEvents_Uid",
                table: "RetrievalEvents",
                column: "Uid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RetrievalEvents");
        }
    }
}
