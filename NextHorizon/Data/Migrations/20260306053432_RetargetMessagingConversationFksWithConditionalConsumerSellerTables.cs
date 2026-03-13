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
            EnsureIntMessagingTables(migrationBuilder);

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

        private static void EnsureIntMessagingTables(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[dbo].[MessagingConversations]', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.MessagingConversations
                    (
                        ConversationId INT NOT NULL IDENTITY(1,1),
                        BuyerUserId INT NOT NULL,
                        SellerUserId INT NOT NULL,
                        ContextType TINYINT NOT NULL,
                        OrderId INT NULL,
                        LastMessageAt DATETIME2 NULL,
                        BuyerLastReadAt DATETIME2 NULL,
                        SellerLastReadAt DATETIME2 NULL,
                        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_MessagingConversations_CreatedAt DEFAULT SYSUTCDATETIME(),
                        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_MessagingConversations_UpdatedAt DEFAULT SYSUTCDATETIME(),
                        CONSTRAINT PK_MessagingConversations PRIMARY KEY (ConversationId)
                    );
                END;

                IF OBJECT_ID(N'[dbo].[MessagingMessages]', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.MessagingMessages
                    (
                        MessageId BIGINT NOT NULL IDENTITY(1,1),
                        ConversationId INT NOT NULL,
                        SenderUserId INT NOT NULL,
                        Body NVARCHAR(2000) NOT NULL,
                        AttachmentUrl NVARCHAR(400) NULL,
                        SentAt DATETIME2 NOT NULL CONSTRAINT DF_MessagingMessages_SentAt DEFAULT SYSUTCDATETIME(),
                        IsDeleted BIT NOT NULL CONSTRAINT DF_MessagingMessages_IsDeleted DEFAULT ((0)),
                        CONSTRAINT PK_MessagingMessages PRIMARY KEY (MessageId)
                    );
                END;

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.columns c
                    INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
                    WHERE c.object_id = OBJECT_ID(N'[dbo].[MessagingConversations]', N'U')
                      AND c.name = N'BuyerUserId'
                      AND t.name = N'int'
                      AND c.is_nullable = 0
                )
                    THROW 51080, 'Migration blocked: [dbo].[MessagingConversations].[BuyerUserId] must be NOT NULL INT before consumer/seller FK retarget.', 1;

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.columns c
                    INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
                    WHERE c.object_id = OBJECT_ID(N'[dbo].[MessagingConversations]', N'U')
                      AND c.name = N'SellerUserId'
                      AND t.name = N'int'
                      AND c.is_nullable = 0
                )
                    THROW 51081, 'Migration blocked: [dbo].[MessagingConversations].[SellerUserId] must be NOT NULL INT before consumer/seller FK retarget.', 1;

                IF NOT EXISTS
                (
                    SELECT 1
                    FROM sys.columns c
                    INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
                    WHERE c.object_id = OBJECT_ID(N'[dbo].[MessagingMessages]', N'U')
                      AND c.name = N'SenderUserId'
                      AND t.name = N'int'
                      AND c.is_nullable = 0
                )
                    THROW 51082, 'Migration blocked: [dbo].[MessagingMessages].[SenderUserId] must be NOT NULL INT before consumer/seller FK retarget.', 1;

                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MessagingMessages_MessagingConversations_ConversationId')
                    ALTER TABLE dbo.MessagingMessages WITH CHECK
                        ADD CONSTRAINT FK_MessagingMessages_MessagingConversations_ConversationId
                        FOREIGN KEY (ConversationId) REFERENCES dbo.MessagingConversations(ConversationId) ON DELETE CASCADE;

                IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
                   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MessagingConversations_User_BuyerUserId')
                    ALTER TABLE dbo.MessagingConversations WITH CHECK
                        ADD CONSTRAINT FK_MessagingConversations_User_BuyerUserId
                        FOREIGN KEY (BuyerUserId) REFERENCES dbo.Users(user_id) ON DELETE NO ACTION;

                IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
                   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MessagingConversations_User_SellerUserId')
                    ALTER TABLE dbo.MessagingConversations WITH CHECK
                        ADD CONSTRAINT FK_MessagingConversations_User_SellerUserId
                        FOREIGN KEY (SellerUserId) REFERENCES dbo.Users(user_id) ON DELETE NO ACTION;

                IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
                   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MessagingMessages_User_SenderUserId')
                    ALTER TABLE dbo.MessagingMessages WITH CHECK
                        ADD CONSTRAINT FK_MessagingMessages_User_SenderUserId
                        FOREIGN KEY (SenderUserId) REFERENCES dbo.Users(user_id) ON DELETE NO ACTION;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[MessagingConversations]', N'U') AND name = N'IX_MessagingConversations_BuyerUserId')
                    CREATE INDEX IX_MessagingConversations_BuyerUserId ON dbo.MessagingConversations (BuyerUserId);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[MessagingConversations]', N'U') AND name = N'IX_MessagingConversations_BuyerUserId_SellerUserId_ContextType')
                    CREATE UNIQUE INDEX IX_MessagingConversations_BuyerUserId_SellerUserId_ContextType
                        ON dbo.MessagingConversations (BuyerUserId, SellerUserId, ContextType)
                        WHERE [ContextType] = 1;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[MessagingConversations]', N'U') AND name = N'IX_MessagingConversations_LastMessageAt')
                    CREATE INDEX IX_MessagingConversations_LastMessageAt ON dbo.MessagingConversations (LastMessageAt);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[MessagingConversations]', N'U') AND name = N'IX_MessagingConversations_OrderId_ContextType')
                    CREATE UNIQUE INDEX IX_MessagingConversations_OrderId_ContextType
                        ON dbo.MessagingConversations (OrderId, ContextType)
                        WHERE [ContextType] = 2;

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[MessagingConversations]', N'U') AND name = N'IX_MessagingConversations_SellerUserId')
                    CREATE INDEX IX_MessagingConversations_SellerUserId ON dbo.MessagingConversations (SellerUserId);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[MessagingMessages]', N'U') AND name = N'IX_MessagingMessages_ConversationId_SentAt')
                    CREATE INDEX IX_MessagingMessages_ConversationId_SentAt ON dbo.MessagingMessages (ConversationId ASC, SentAt DESC);

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[MessagingMessages]', N'U') AND name = N'IX_MessagingMessages_SenderUserId')
                    CREATE INDEX IX_MessagingMessages_SenderUserId ON dbo.MessagingMessages (SenderUserId);
                """);
        }
    }
}

