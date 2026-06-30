using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using NextHorizon.Data;

#nullable disable

namespace NextHorizon.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260327114500_NormalizeQueuedLiveAgentStatuses")]
    public partial class NormalizeQueuedLiveAgentStatuses : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE session
                SET [Status] = 1
                FROM [LiveAgentSessions] AS session
                WHERE [Status] = 2;
                """);

            migrationBuilder.Sql(
                """
                UPDATE supportFaq
                SET [Status] = 'Waiting',
                    [StartTime] = NULL,
                    [DurationMinutes] = 0
                FROM [dbo].[SupportFAQs] AS supportFaq
                INNER JOIN [LiveAgentSessions] AS session
                    ON session.[SupportFaqId] = supportFaq.[Id]
                WHERE session.[Status] = 1
                    AND supportFaq.[Status] = 'Active';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE supportFaq
                SET [Status] = 'Active',
                    [StartTime] = COALESCE([StartTime], [CreatedAt])
                FROM [dbo].[SupportFAQs] AS supportFaq
                INNER JOIN [LiveAgentSessions] AS session
                    ON session.[SupportFaqId] = supportFaq.[Id]
                WHERE session.[Status] = 1
                    AND supportFaq.[Status] = 'Waiting'
                    AND LTRIM(RTRIM(COALESCE(session.[FirstQuestion], ''))) <> '';
                """);

            migrationBuilder.Sql(
                """
                UPDATE session
                SET [Status] = 2
                FROM [LiveAgentSessions] AS session
                INNER JOIN [dbo].[SupportFAQs] AS supportFaq
                    ON supportFaq.[Id] = session.[SupportFaqId]
                WHERE session.[Status] = 1
                    AND supportFaq.[Status] = 'Active'
                    AND LTRIM(RTRIM(COALESCE(session.[FirstQuestion], ''))) <> '';
                """);
        }
    }
}
