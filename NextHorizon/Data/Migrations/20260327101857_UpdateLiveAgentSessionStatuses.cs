using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextHorizon.Migrations
{
    /// <inheritdoc />
    public partial class UpdateLiveAgentSessionStatuses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE [dbo].[SupportFAQs]
                SET [Resolution] = CASE
                    WHEN [Resolution] = 'Yes' THEN 'Resolved'
                    WHEN [Resolution] = 'No' AND LTRIM(RTRIM(COALESCE([Question], ''))) = '' THEN 'Waiting'
                    WHEN [Resolution] = 'No' THEN 'Active'
                    ELSE [Resolution]
                END
                WHERE [Resolution] IN ('Yes', 'No');
                """);

            migrationBuilder.Sql(
                """
                UPDATE [LiveAgentSessions]
                SET [Status] = CASE
                    WHEN [Status] = 2 THEN 3
                    WHEN [Status] = 1 AND LTRIM(RTRIM(COALESCE([FirstQuestion], ''))) = '' THEN 1
                    WHEN [Status] = 1 THEN 2
                    ELSE [Status]
                END
                WHERE [Status] IN (1, 2);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE [dbo].[SupportFAQs]
                SET [Resolution] = CASE
                    WHEN [Resolution] = 'Resolved' THEN 'Yes'
                    WHEN [Resolution] IN ('Waiting', 'Active') THEN 'No'
                    ELSE [Resolution]
                END
                WHERE [Resolution] IN ('Resolved', 'Waiting', 'Active');
                """);

            migrationBuilder.Sql(
                """
                UPDATE [LiveAgentSessions]
                SET [Status] = CASE
                    WHEN [Status] = 3 THEN 2
                    WHEN [Status] IN (1, 2) THEN 1
                    ELSE [Status]
                END
                WHERE [Status] IN (1, 2, 3);
                """);
        }
    }
}
