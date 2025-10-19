using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EpiSense.Analysis.Migrations
{
    /// <inheritdoc />
    public partial class InitialAnalysisSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "observation_summaries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    observation_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    data_coleta = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    codigo_municipio_ibge = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    flags = table.Column<List<string>>(type: "jsonb", nullable: false),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    raw_data_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    lab_values = table.Column<Dictionary<string, decimal>>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_observation_summaries", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_observation_summaries_codigo_municipio_ibge",
                table: "observation_summaries",
                column: "codigo_municipio_ibge");

            migrationBuilder.CreateIndex(
                name: "IX_observation_summaries_data_coleta",
                table: "observation_summaries",
                column: "data_coleta");

            migrationBuilder.CreateIndex(
                name: "IX_observation_summaries_processed_at",
                table: "observation_summaries",
                column: "processed_at");

            migrationBuilder.CreateIndex(
                name: "IX_observation_summaries_raw_data_id",
                table: "observation_summaries",
                column: "raw_data_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "observation_summaries");
        }
    }
}
