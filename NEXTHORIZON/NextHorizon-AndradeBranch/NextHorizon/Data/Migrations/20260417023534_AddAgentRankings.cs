using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextHorizon.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentRankings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "AgentRankings",
                schema: "dbo",
                columns: table => new
                {
                    AgentRankingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AgentUserId = table.Column<int>(type: "int", nullable: false),
                    PeriodStartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AverageQaScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ReviewCount = table.Column<int>(type: "int", nullable: false),
                    RankPosition = table.Column<int>(type: "int", nullable: false),
                    RankedAgentCount = table.Column<int>(type: "int", nullable: false),
                    CalculatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentRankings", x => x.AgentRankingId);
                });

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

            migrationBuilder.Sql(
                """
                ;WITH ReviewScores AS (
                    SELECT
                        CAST(DATEFROMPARTS(YEAR(review.[CreatedAtUtc]), MONTH(review.[CreatedAtUtc]), 1) AS datetime2) AS PeriodStartUtc,
                        supportFaq.[AgentId] AS AgentUserId,
                        CAST(review.[OverallPercent] AS decimal(10,4)) AS OverallPercent
                    FROM [QaReviews] AS review
                    INNER JOIN [dbo].[SupportFAQs] AS supportFaq
                        ON supportFaq.[Id] = review.[SupportFaqId]
                    WHERE supportFaq.[AgentId] IS NOT NULL
                ),
                AgentScores AS (
                    SELECT
                        PeriodStartUtc,
                        AgentUserId,
                        CAST(ROUND(AVG(OverallPercent), 2) AS decimal(5,2)) AS AverageQaScore,
                        CAST(COUNT(1) AS int) AS ReviewCount
                    FROM ReviewScores
                    GROUP BY PeriodStartUtc, AgentUserId
                ),
                RankedScores AS (
                    SELECT
                        PeriodStartUtc,
                        AgentUserId,
                        AverageQaScore,
                        ReviewCount,
                        RANK() OVER (PARTITION BY PeriodStartUtc ORDER BY AverageQaScore DESC) AS RankPosition,
                        COUNT(1) OVER (PARTITION BY PeriodStartUtc) AS RankedAgentCount
                    FROM AgentScores
                )
                INSERT INTO [dbo].[AgentRankings] (
                    [AgentUserId],
                    [PeriodStartUtc],
                    [AverageQaScore],
                    [ReviewCount],
                    [RankPosition],
                    [RankedAgentCount],
                    [CalculatedAtUtc],
                    [UpdatedAtUtc]
                )
                SELECT
                    AgentUserId,
                    PeriodStartUtc,
                    AverageQaScore,
                    ReviewCount,
                    RankPosition,
                    RankedAgentCount,
                    SYSUTCDATETIME(),
                    SYSUTCDATETIME()
                FROM RankedScores;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentRankings",
                schema: "dbo");
        }
    }
}
