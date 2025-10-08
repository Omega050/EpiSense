using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EpiSense.Analysis.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "analysis");

            migrationBuilder.CreateTable(
                name: "analysis_results",
                schema: "analysis",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnalysisType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AnalyzedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Region = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CasesCount = table.Column<int>(type: "integer", nullable: false),
                    RiskScore = table.Column<double>(type: "double precision", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analysis_results", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_analysis_results_AnalysisType",
                schema: "analysis",
                table: "analysis_results",
                column: "AnalysisType");

            migrationBuilder.CreateIndex(
                name: "IX_analysis_results_AnalyzedAt",
                schema: "analysis",
                table: "analysis_results",
                column: "AnalyzedAt");

            migrationBuilder.CreateIndex(
                name: "IX_analysis_results_Region",
                schema: "analysis",
                table: "analysis_results",
                column: "Region");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "analysis_results",
                schema: "analysis");
        }
    }
}
