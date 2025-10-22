using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EpiSense.Analysis.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProcessedAtAndRawDataId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_observation_summaries_processed_at",
                table: "observation_summaries");

            migrationBuilder.DropIndex(
                name: "IX_observation_summaries_raw_data_id",
                table: "observation_summaries");

            migrationBuilder.DropColumn(
                name: "processed_at",
                table: "observation_summaries");

            migrationBuilder.DropColumn(
                name: "raw_data_id",
                table: "observation_summaries");

            migrationBuilder.CreateIndex(
                name: "IX_observation_summaries_flags",
                table: "observation_summaries",
                column: "flags")
                .Annotation("Npgsql:IndexMethod", "gin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_observation_summaries_flags",
                table: "observation_summaries");

            migrationBuilder.AddColumn<DateTime>(
                name: "processed_at",
                table: "observation_summaries",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "raw_data_id",
                table: "observation_summaries",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_observation_summaries_processed_at",
                table: "observation_summaries",
                column: "processed_at");

            migrationBuilder.CreateIndex(
                name: "IX_observation_summaries_raw_data_id",
                table: "observation_summaries",
                column: "raw_data_id");
        }
    }
}
