using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextHorizon.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentRankingMetricTypeAndAht : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AgentRankings_PeriodStartUtc_AgentUserId",
                schema: "dbo",
                table: "AgentRankings");

            migrationBuilder.DropIndex(
                name: "IX_AgentRankings_PeriodStartUtc_RankPosition",
                schema: "dbo",
                table: "AgentRankings");

            migrationBuilder.RenameColumn(
                name: "AverageQaScore",
                schema: "dbo",
                table: "AgentRankings",
                newName: "MetricValue");

            migrationBuilder.AlterColumn<decimal>(
                name: "MetricValue",
                schema: "dbo",
                table: "AgentRankings",
                type: "decimal(10,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)");

            migrationBuilder.AddColumn<byte>(
                name: "MetricType",
                schema: "dbo",
                table: "AgentRankings",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)1);

            migrationBuilder.CreateIndex(
                name: "IX_AgentRankings_PeriodStartUtc_MetricType_AgentUserId",
                schema: "dbo",
                table: "AgentRankings",
                columns: new[] { "PeriodStartUtc", "MetricType", "AgentUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentRankings_PeriodStartUtc_MetricType_RankPosition",
                schema: "dbo",
                table: "AgentRankings",
                columns: new[] { "PeriodStartUtc", "MetricType", "RankPosition" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AgentRankings_PeriodStartUtc_MetricType_AgentUserId",
                schema: "dbo",
                table: "AgentRankings");

            migrationBuilder.DropIndex(
                name: "IX_AgentRankings_PeriodStartUtc_MetricType_RankPosition",
                schema: "dbo",
                table: "AgentRankings");

            migrationBuilder.DropColumn(
                name: "MetricType",
                schema: "dbo",
                table: "AgentRankings");

            migrationBuilder.AlterColumn<decimal>(
                name: "MetricValue",
                schema: "dbo",
                table: "AgentRankings",
                type: "decimal(5,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)");

            migrationBuilder.RenameColumn(
                name: "MetricValue",
                schema: "dbo",
                table: "AgentRankings",
                newName: "AverageQaScore");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRankings_PeriodStartUtc_AgentUserId",
                schema: "dbo",
                table: "AgentRankings",
                columns: new[] { "PeriodStartUtc", "AgentUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentRankings_PeriodStartUtc_RankPosition",
                schema: "dbo",
                table: "AgentRankings",
                columns: new[] { "PeriodStartUtc", "RankPosition" });
        }
    }
}
