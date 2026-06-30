using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextHorizon.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnforceSingleActiveLiveAgentSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DECLARE @ResolvedStatus tinyint = 3;
                DECLARE @Timestamp datetime2 = SYSUTCDATETIME();
                DECLARE @DuplicateSupportFaqIds TABLE ([SupportFaqId] int NOT NULL PRIMARY KEY);

                WITH ranked_sessions AS (
                    SELECT
                        session.[LiveAgentSessionId],
                        session.[SupportFaqId],
                        ROW_NUMBER() OVER (
                            PARTITION BY session.[UserId]
                            ORDER BY session.[CreatedAt] DESC, session.[LiveAgentSessionId] DESC
                        ) AS [row_number]
                    FROM [LiveAgentSessions] AS session
                    WHERE session.[Status] <> @ResolvedStatus
                )
                INSERT INTO @DuplicateSupportFaqIds ([SupportFaqId])
                SELECT ranked.[SupportFaqId]
                FROM ranked_sessions AS ranked
                WHERE ranked.[row_number] > 1;

                UPDATE session
                SET
                    session.[Status] = @ResolvedStatus,
                    session.[UpdatedAt] = @Timestamp
                FROM [LiveAgentSessions] AS session
                INNER JOIN @DuplicateSupportFaqIds AS duplicates
                    ON duplicates.[SupportFaqId] = session.[SupportFaqId];

                UPDATE supportFaq
                SET
                    supportFaq.[Status] = 'Resolved',
                    supportFaq.[EndTime] = COALESCE(supportFaq.[EndTime], @Timestamp),
                    supportFaq.[DurationMinutes] = CASE
                        WHEN supportFaq.[StartTime] IS NULL THEN 0
                        ELSE CASE
                            WHEN DATEDIFF(MINUTE, supportFaq.[StartTime], COALESCE(supportFaq.[EndTime], @Timestamp)) < 0 THEN 0
                            ELSE DATEDIFF(MINUTE, supportFaq.[StartTime], COALESCE(supportFaq.[EndTime], @Timestamp))
                        END
                    END
                FROM [dbo].[SupportFAQs] AS supportFaq
                INNER JOIN @DuplicateSupportFaqIds AS duplicates
                    ON duplicates.[SupportFaqId] = supportFaq.[Id];
                """);

            migrationBuilder.CreateIndex(
                name: "IX_LiveAgentSessions_UserId",
                table: "LiveAgentSessions",
                column: "UserId",
                unique: true,
                filter: "[Status] <> 3");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LiveAgentSessions_UserId",
                table: "LiveAgentSessions");
        }
    }
}
