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
                SET [Status] = CASE
                    WHEN [Status] = 'Yes' THEN 'Resolved'
                    WHEN [Status] = 'No' THEN 'Waiting'
                    ELSE [Status]
                END
                WHERE [Status] IN ('Yes', 'No');
                """);

            migrationBuilder.Sql(
                """
                UPDATE [dbo].[SupportFAQs]
                SET [StartTime] = CASE
                        WHEN [Status] IN ('Active', 'Resolved') AND [StartTime] IS NULL THEN [CreatedAt]
                        ELSE [StartTime]
                    END,
                    [EndTime] = CASE
                        WHEN [Status] = 'Resolved' AND [EndTime] IS NULL THEN [CreatedAt]
                        ELSE [EndTime]
                    END
                WHERE [Status] IN ('Active', 'Resolved');
                """);

            migrationBuilder.Sql(
                """
                UPDATE [dbo].[SupportFAQs]
                SET [StartTime] = NULL,
                    [DurationMinutes] = 0
                WHERE [Status] = 'Waiting';
                """);

            migrationBuilder.Sql(
                """
                UPDATE [LiveAgentSessions]
                SET [Status] = CASE
                    WHEN [Status] = 2 THEN 3
                    WHEN [Status] = 1 THEN 1
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
                SET [Status] = CASE
                    WHEN [Status] = 'Resolved' THEN 'Yes'
                    WHEN [Status] IN ('Waiting', 'Active') THEN 'No'
                    ELSE [Status]
                END
                WHERE [Status] IN ('Resolved', 'Waiting', 'Active');
                """);

            migrationBuilder.Sql(
                """
                UPDATE [dbo].[SupportFAQs]
                SET [StartTime] = NULL,
                    [EndTime] = NULL
                WHERE [Status] IN ('Yes', 'No');
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
