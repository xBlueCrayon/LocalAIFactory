using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LocalAIFactory.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceArtifactEnvelope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceReference",
                table: "IngestionJobs",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceRevision",
                table: "IngestionJobs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceSystem",
                table: "IngestionJobs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "Uid",
                table: "IngestionJobs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "DetectedLanguage",
                table: "ImportedFiles",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceSystem",
                table: "ImportedFiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "Uid",
                table: "ImportedFiles",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // KE-007: assign a unique Uid to pre-existing rows before the unique indexes are created
            // (existing rows default to Guid.Empty). Local rows predate export, so NEWID() is sufficient;
            // new rows use app-side Guid v7.
            migrationBuilder.Sql("UPDATE [IngestionJobs] SET [Uid] = NEWID() WHERE [Uid] = '00000000-0000-0000-0000-000000000000';");
            migrationBuilder.Sql("UPDATE [ImportedFiles] SET [Uid] = NEWID() WHERE [Uid] = '00000000-0000-0000-0000-000000000000';");

            migrationBuilder.CreateIndex(
                name: "IX_IngestionJobs_Uid",
                table: "IngestionJobs",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ImportedFiles_Uid",
                table: "ImportedFiles",
                column: "Uid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IngestionJobs_Uid",
                table: "IngestionJobs");

            migrationBuilder.DropIndex(
                name: "IX_ImportedFiles_Uid",
                table: "ImportedFiles");

            migrationBuilder.DropColumn(
                name: "SourceReference",
                table: "IngestionJobs");

            migrationBuilder.DropColumn(
                name: "SourceRevision",
                table: "IngestionJobs");

            migrationBuilder.DropColumn(
                name: "SourceSystem",
                table: "IngestionJobs");

            migrationBuilder.DropColumn(
                name: "Uid",
                table: "IngestionJobs");

            migrationBuilder.DropColumn(
                name: "DetectedLanguage",
                table: "ImportedFiles");

            migrationBuilder.DropColumn(
                name: "SourceSystem",
                table: "ImportedFiles");

            migrationBuilder.DropColumn(
                name: "Uid",
                table: "ImportedFiles");
        }
    }
}
