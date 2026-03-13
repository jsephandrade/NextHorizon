using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextHorizon.Migrations
{
    /// <inheritdoc />
    public partial class RetargetMessagingConversationFksWithConditionalConsumerSellerTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MessagingConversations_User_BuyerUserId')
                    ALTER TABLE dbo.MessagingConversations DROP CONSTRAINT FK_MessagingConversations_User_BuyerUserId;

                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MessagingConversations_Users_BuyerUserId')
                    ALTER TABLE dbo.MessagingConversations DROP CONSTRAINT FK_MessagingConversations_Users_BuyerUserId;

                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MessagingConversations_User_SellerUserId')
                    ALTER TABLE dbo.MessagingConversations DROP CONSTRAINT FK_MessagingConversations_User_SellerUserId;

                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MessagingConversations_Users_SellerUserId')
                    ALTER TABLE dbo.MessagingConversations DROP CONSTRAINT FK_MessagingConversations_Users_SellerUserId;
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

                IF OBJECT_ID(N'[dbo].[Sellers]', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.Sellers
                    (
                        seller_id INT NOT NULL,
                        CONSTRAINT PK_Sellers_seller_id PRIMARY KEY (seller_id)
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
                    THROW 51040, 'Migration blocked: [dbo].[Consumers].[consumer_id] must be NOT NULL INT.', 1;

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.columns c
                    INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
                    WHERE c.object_id = OBJECT_ID(N'[dbo].[Sellers]', N'U')
                      AND c.name = N'seller_id'
                      AND t.name = N'int'
                      AND c.is_nullable = 0
                )
                    THROW 51041, 'Migration blocked: [dbo].[Sellers].[seller_id] must be NOT NULL INT.', 1;

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
                    THROW 51042, 'Migration blocked: [dbo].[Consumers].[consumer_id] must have a single-column UNIQUE or PRIMARY KEY index.', 1;

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
                    WHERE i.object_id = OBJECT_ID(N'[dbo].[Sellers]', N'U')
                      AND i.is_hypothetical = 0
                      AND (i.is_primary_key = 1 OR i.is_unique = 1 OR i.is_unique_constraint = 1)
                    GROUP BY i.index_id
                    HAVING COUNT(*) = 1
                       AND MAX(CASE WHEN c.name = N'seller_id' THEN 1 ELSE 0 END) = 1
                )
                    THROW 51043, 'Migration blocked: [dbo].[Sellers].[seller_id] must have a single-column UNIQUE or PRIMARY KEY index.', 1;

                DELETE mm
                FROM dbo.MessagingMessages mm
                INNER JOIN dbo.MessagingConversations mc ON mc.ConversationId = mm.ConversationId
                LEFT JOIN dbo.Consumers c ON c.consumer_id = mc.BuyerUserId
                LEFT JOIN dbo.Sellers s ON s.seller_id = mc.SellerUserId
                WHERE c.consumer_id IS NULL OR s.seller_id IS NULL;

                DELETE mc
                FROM dbo.MessagingConversations mc
                LEFT JOIN dbo.Consumers c ON c.consumer_id = mc.BuyerUserId
                LEFT JOIN dbo.Sellers s ON s.seller_id = mc.SellerUserId
                WHERE c.consumer_id IS NULL OR s.seller_id IS NULL;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_MessagingConversations_Consumers_BuyerUserId",
                table: "MessagingConversations",
                column: "BuyerUserId",
                principalSchema: "dbo",
                principalTable: "Consumers",
                principalColumn: "consumer_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MessagingConversations_Sellers_SellerUserId",
                table: "MessagingConversations",
                column: "SellerUserId",
                principalSchema: "dbo",
                principalTable: "Sellers",
                principalColumn: "seller_id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MessagingConversations_Consumers_BuyerUserId')
                    ALTER TABLE dbo.MessagingConversations DROP CONSTRAINT FK_MessagingConversations_Consumers_BuyerUserId;

                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MessagingConversations_Sellers_SellerUserId')
                    ALTER TABLE dbo.MessagingConversations DROP CONSTRAINT FK_MessagingConversations_Sellers_SellerUserId;

                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MessagingConversations_User_BuyerUserId')
                    ALTER TABLE dbo.MessagingConversations DROP CONSTRAINT FK_MessagingConversations_User_BuyerUserId;

                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MessagingConversations_User_SellerUserId')
                    ALTER TABLE dbo.MessagingConversations DROP CONSTRAINT FK_MessagingConversations_User_SellerUserId;

                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MessagingConversations_Users_BuyerUserId')
                    ALTER TABLE dbo.MessagingConversations DROP CONSTRAINT FK_MessagingConversations_Users_BuyerUserId;

                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MessagingConversations_Users_SellerUserId')
                    ALTER TABLE dbo.MessagingConversations DROP CONSTRAINT FK_MessagingConversations_Users_SellerUserId;
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_MessagingConversations_Users_BuyerUserId",
                table: "MessagingConversations",
                column: "BuyerUserId",
                principalSchema: "dbo",
                principalTable: "Users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MessagingConversations_Users_SellerUserId",
                table: "MessagingConversations",
                column: "SellerUserId",
                principalSchema: "dbo",
                principalTable: "Users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}

