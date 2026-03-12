using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemberTracker.Migrations
{
    /// <inheritdoc />
    public partial class RetargetMemberUploadsUserIdToConsumersWithConditionalTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MemberUploads_User_UserId')
                    ALTER TABLE dbo.MemberUploads DROP CONSTRAINT FK_MemberUploads_User_UserId;

                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MemberUploads_Users_UserId')
                    ALTER TABLE dbo.MemberUploads DROP CONSTRAINT FK_MemberUploads_Users_UserId;
                """);

            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[dbo].[Consumers]', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.Consumers
                    (
                        consumer_id INT NOT NULL,
                        CONSTRAINT PK_Consumers_consumer_id PRIMARY KEY (consumer_id)
                    );
                END;

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.columns c
                    INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
                    WHERE c.object_id = OBJECT_ID(N'[dbo].[Consumers]', N'U')
                      AND c.name = N'consumer_id'
                      AND t.name = N'int'
                      AND c.is_nullable = 0
                )
                    THROW 51050, 'Migration blocked: [dbo].[Consumers].[consumer_id] must be NOT NULL INT.', 1;

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.indexes i
                    INNER JOIN sys.index_columns ic
                        ON ic.object_id = i.object_id
                       AND ic.index_id = i.index_id
                    INNER JOIN sys.columns c
                        ON c.object_id = ic.object_id
                       AND c.column_id = ic.column_id
                    WHERE i.object_id = OBJECT_ID(N'[dbo].[Consumers]', N'U')
                      AND i.is_hypothetical = 0
                      AND (i.is_primary_key = 1 OR i.is_unique = 1 OR i.is_unique_constraint = 1)
                    GROUP BY i.index_id
                    HAVING COUNT(*) = 1
                       AND MAX(CASE WHEN c.name = N'consumer_id' THEN 1 ELSE 0 END) = 1
                )
                    THROW 51051, 'Migration blocked: [dbo].[Consumers].[consumer_id] must have a single-column UNIQUE or PRIMARY KEY index.', 1;

                DELETE mu
                FROM dbo.MemberUploads mu
                LEFT JOIN dbo.Consumers c ON c.consumer_id = mu.UserId
                WHERE c.consumer_id IS NULL;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_MemberUploads_Consumers_UserId",
                table: "MemberUploads",
                column: "UserId",
                principalSchema: "dbo",
                principalTable: "Consumers",
                principalColumn: "consumer_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MemberUploads_Consumers_UserId')
                    ALTER TABLE dbo.MemberUploads DROP CONSTRAINT FK_MemberUploads_Consumers_UserId;

                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MemberUploads_User_UserId')
                    ALTER TABLE dbo.MemberUploads DROP CONSTRAINT FK_MemberUploads_User_UserId;

                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MemberUploads_Users_UserId')
                    ALTER TABLE dbo.MemberUploads DROP CONSTRAINT FK_MemberUploads_Users_UserId;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_MemberUploads_Users_UserId",
                table: "MemberUploads",
                column: "UserId",
                principalSchema: "dbo",
                principalTable: "Users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
