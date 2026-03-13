using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NextHorizon.Migrations
{
    /// <inheritdoc />
    public partial class AddMessagingModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MessagingConversations",
                columns: table => new
                {
                    ConversationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BuyerUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SellerUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ContextType = table.Column<byte>(type: "tinyint", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    LastMessageAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BuyerLastReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SellerLastReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessagingConversations", x => x.ConversationId);
                    table.CheckConstraint("CK_MessagingConversations_ContextType", "[ContextType] IN (1, 2)");
                    table.CheckConstraint("CK_MessagingConversations_ContextType_Order", "([ContextType] = 1 AND [OrderId] IS NULL) OR ([ContextType] = 2 AND [OrderId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_MessagingConversations_AspNetUsers_BuyerUserId",
                        column: x => x.BuyerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MessagingConversations_AspNetUsers_SellerUserId",
                        column: x => x.SellerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MessagingMessages",
                columns: table => new
                {
                    MessageId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConversationId = table.Column<int>(type: "int", nullable: false),
                    SenderUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Body = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AttachmentUrl = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessagingMessages", x => x.MessageId);
                    table.ForeignKey(
                        name: "FK_MessagingMessages_AspNetUsers_SenderUserId",
                        column: x => x.SenderUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MessagingMessages_MessagingConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalTable: "MessagingConversations",
                        principalColumn: "ConversationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MessagingConversations_BuyerUserId",
                table: "MessagingConversations",
                column: "BuyerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MessagingConversations_BuyerUserId_SellerUserId_ContextType",
                table: "MessagingConversations",
                columns: new[] { "BuyerUserId", "SellerUserId", "ContextType" },
                unique: true,
                filter: "[ContextType] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_MessagingConversations_LastMessageAt",
                table: "MessagingConversations",
                column: "LastMessageAt");

            migrationBuilder.CreateIndex(
                name: "IX_MessagingConversations_OrderId_ContextType",
                table: "MessagingConversations",
                columns: new[] { "OrderId", "ContextType" },
                unique: true,
                filter: "[ContextType] = 2");

            migrationBuilder.CreateIndex(
                name: "IX_MessagingConversations_SellerUserId",
                table: "MessagingConversations",
                column: "SellerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MessagingMessages_ConversationId_SentAt",
                table: "MessagingMessages",
                columns: new[] { "ConversationId", "SentAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_MessagingMessages_SenderUserId",
                table: "MessagingMessages",
                column: "SenderUserId");

            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.sp_MessageConversation_GetById
                    @ConversationID INT,
                    @UserID NVARCHAR(450)
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF NOT EXISTS (
                        SELECT 1
                        FROM dbo.MessagingConversations
                        WHERE ConversationId = @ConversationID
                          AND (BuyerUserId = @UserID OR SellerUserId = @UserID)
                    )
                    BEGIN
                        RETURN;
                    END

                    SELECT
                        c.ConversationId AS ConversationID,
                        c.BuyerUserId AS BuyerUserID,
                        c.SellerUserId AS SellerUserID,
                        c.ContextType AS ContextType,
                        c.OrderId AS OrderID,
                        c.LastMessageAt AS LastMessageAt,
                        c.BuyerLastReadAt AS BuyerLastReadAt,
                        c.SellerLastReadAt AS SellerLastReadAt,
                        CASE
                            WHEN lm.MessageId IS NULL THEN NULL
                            WHEN lm.IsDeleted = 1 THEN N'[deleted]'
                            WHEN LEN(lm.Body) > 120 THEN LEFT(lm.Body, 117) + N'...'
                            ELSE lm.Body
                        END AS LastMessagePreview,
                        (
                            SELECT COUNT(1)
                            FROM dbo.MessagingMessages m
                            WHERE m.ConversationId = c.ConversationId
                              AND m.IsDeleted = 0
                              AND m.SenderUserId <> @UserID
                              AND m.SentAt > COALESCE(
                                    CASE WHEN c.BuyerUserId = @UserID THEN c.BuyerLastReadAt ELSE c.SellerLastReadAt END,
                                    CONVERT(DATETIME2, '1900-01-01')
                              )
                        ) AS UnreadCount,
                        c.CreatedAt AS CreatedAt,
                        c.UpdatedAt AS UpdatedAt
                    FROM dbo.MessagingConversations c
                    OUTER APPLY
                    (
                        SELECT TOP (1)
                            m.MessageId,
                            m.Body,
                            m.IsDeleted
                        FROM dbo.MessagingMessages m
                        WHERE m.ConversationId = c.ConversationId
                        ORDER BY m.SentAt DESC, m.MessageId DESC
                    ) lm
                    WHERE c.ConversationId = @ConversationID;
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.sp_MessageConversation_CreateOrGet_General
                    @BuyerUserID NVARCHAR(450),
                    @SellerUserID NVARCHAR(450)
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF @BuyerUserID = @SellerUserID
                    BEGIN
                        THROW 50001, 'Buyer and seller must be different users.', 1;
                    END

                    DECLARE @ConversationID INT;

                    SELECT TOP (1) @ConversationID = ConversationId
                    FROM dbo.MessagingConversations
                    WHERE BuyerUserId = @BuyerUserID
                      AND SellerUserId = @SellerUserID
                      AND ContextType = 1;

                    IF @ConversationID IS NULL
                    BEGIN
                        INSERT INTO dbo.MessagingConversations
                        (
                            BuyerUserId,
                            SellerUserId,
                            ContextType,
                            OrderId,
                            LastMessageAt,
                            BuyerLastReadAt,
                            SellerLastReadAt,
                            CreatedAt,
                            UpdatedAt
                        )
                        VALUES
                        (
                            @BuyerUserID,
                            @SellerUserID,
                            1,
                            NULL,
                            NULL,
                            NULL,
                            NULL,
                            SYSUTCDATETIME(),
                            SYSUTCDATETIME()
                        );

                        SET @ConversationID = CAST(SCOPE_IDENTITY() AS INT);
                    END

                    EXEC dbo.sp_MessageConversation_GetById
                        @ConversationID = @ConversationID,
                        @UserID = @BuyerUserID;
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.sp_MessageConversation_CreateOrGet_Order
                    @OrderID INT,
                    @BuyerUserID NVARCHAR(450),
                    @SellerUserID NVARCHAR(450)
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF @BuyerUserID = @SellerUserID
                    BEGIN
                        THROW 50002, 'Buyer and seller must be different users for order conversations.', 1;
                    END

                    DECLARE @ConversationID INT;

                    SELECT TOP (1) @ConversationID = ConversationId
                    FROM dbo.MessagingConversations
                    WHERE OrderId = @OrderID
                      AND ContextType = 2;

                    IF @ConversationID IS NULL
                    BEGIN
                        INSERT INTO dbo.MessagingConversations
                        (
                            BuyerUserId,
                            SellerUserId,
                            ContextType,
                            OrderId,
                            LastMessageAt,
                            BuyerLastReadAt,
                            SellerLastReadAt,
                            CreatedAt,
                            UpdatedAt
                        )
                        VALUES
                        (
                            @BuyerUserID,
                            @SellerUserID,
                            2,
                            @OrderID,
                            NULL,
                            NULL,
                            NULL,
                            SYSUTCDATETIME(),
                            SYSUTCDATETIME()
                        );

                        SET @ConversationID = CAST(SCOPE_IDENTITY() AS INT);
                    END

                    EXEC dbo.sp_MessageConversation_GetById
                        @ConversationID = @ConversationID,
                        @UserID = @BuyerUserID;
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.sp_MessageConversation_ListByUser
                    @UserID NVARCHAR(450),
                    @Page INT,
                    @PageSize INT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF @Page < 1 SET @Page = 1;
                    IF @PageSize < 1 SET @PageSize = 20;
                    IF @PageSize > 100 SET @PageSize = 100;

                    ;WITH Base AS
                    (
                        SELECT
                            c.ConversationId,
                            c.BuyerUserId,
                            c.SellerUserId,
                            c.ContextType,
                            c.OrderId,
                            c.LastMessageAt,
                            c.BuyerLastReadAt,
                            c.SellerLastReadAt,
                            c.CreatedAt,
                            c.UpdatedAt
                        FROM dbo.MessagingConversations c
                        WHERE c.BuyerUserId = @UserID OR c.SellerUserId = @UserID
                    )
                    SELECT
                        b.ConversationId AS ConversationID,
                        b.BuyerUserId AS BuyerUserID,
                        b.SellerUserId AS SellerUserID,
                        b.ContextType AS ContextType,
                        b.OrderId AS OrderID,
                        b.LastMessageAt AS LastMessageAt,
                        b.BuyerLastReadAt AS BuyerLastReadAt,
                        b.SellerLastReadAt AS SellerLastReadAt,
                        CASE
                            WHEN lm.MessageId IS NULL THEN NULL
                            WHEN lm.IsDeleted = 1 THEN N'[deleted]'
                            WHEN LEN(lm.Body) > 120 THEN LEFT(lm.Body, 117) + N'...'
                            ELSE lm.Body
                        END AS LastMessagePreview,
                        (
                            SELECT COUNT(1)
                            FROM dbo.MessagingMessages m
                            WHERE m.ConversationId = b.ConversationId
                              AND m.IsDeleted = 0
                              AND m.SenderUserId <> @UserID
                              AND m.SentAt > COALESCE(
                                    CASE WHEN b.BuyerUserId = @UserID THEN b.BuyerLastReadAt ELSE b.SellerLastReadAt END,
                                    CONVERT(DATETIME2, '1900-01-01')
                              )
                        ) AS UnreadCount,
                        b.CreatedAt AS CreatedAt,
                        b.UpdatedAt AS UpdatedAt
                    FROM Base b
                    OUTER APPLY
                    (
                        SELECT TOP (1)
                            m.MessageId,
                            m.Body,
                            m.IsDeleted
                        FROM dbo.MessagingMessages m
                        WHERE m.ConversationId = b.ConversationId
                        ORDER BY m.SentAt DESC, m.MessageId DESC
                    ) lm
                    ORDER BY COALESCE(b.LastMessageAt, b.CreatedAt) DESC, b.ConversationId DESC
                    OFFSET (@Page - 1) * @PageSize ROWS
                    FETCH NEXT @PageSize ROWS ONLY;

                    SELECT COUNT(1) AS TotalCount
                    FROM Base;
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.sp_Message_Send
                    @ConversationID INT,
                    @SenderUserID NVARCHAR(450),
                    @Body NVARCHAR(2000),
                    @AttachmentUrl NVARCHAR(400) = NULL
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF @Body IS NULL SET @Body = N'';

                    IF NOT EXISTS (
                        SELECT 1
                        FROM dbo.MessagingConversations
                        WHERE ConversationId = @ConversationID
                          AND (BuyerUserId = @SenderUserID OR SellerUserId = @SenderUserID)
                    )
                    BEGIN
                        RETURN;
                    END

                    DECLARE @SentAt DATETIME2 = SYSUTCDATETIME();

                    INSERT INTO dbo.MessagingMessages
                    (
                        ConversationId,
                        SenderUserId,
                        Body,
                        AttachmentUrl,
                        SentAt,
                        IsDeleted
                    )
                    VALUES
                    (
                        @ConversationID,
                        @SenderUserID,
                        @Body,
                        @AttachmentUrl,
                        @SentAt,
                        0
                    );

                    UPDATE dbo.MessagingConversations
                    SET LastMessageAt = @SentAt,
                        UpdatedAt = @SentAt
                    WHERE ConversationId = @ConversationID;

                    SELECT
                        MessageId AS MessageID,
                        ConversationId AS ConversationID,
                        SenderUserId AS SenderUserID,
                        Body AS Body,
                        AttachmentUrl AS AttachmentUrl,
                        SentAt AS SentAt,
                        IsDeleted AS IsDeleted
                    FROM dbo.MessagingMessages
                    WHERE MessageId = CAST(SCOPE_IDENTITY() AS BIGINT);
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.sp_Message_List
                    @ConversationID INT,
                    @UserID NVARCHAR(450),
                    @Before DATETIME2 = NULL,
                    @PageSize INT = 50
                AS
                BEGIN
                    SET NOCOUNT ON;

                    IF @PageSize < 1 SET @PageSize = 50;
                    IF @PageSize > 100 SET @PageSize = 100;

                    DECLARE @IsMember BIT = 0;

                    IF EXISTS (
                        SELECT 1
                        FROM dbo.MessagingConversations
                        WHERE ConversationId = @ConversationID
                          AND (BuyerUserId = @UserID OR SellerUserId = @UserID)
                    )
                    BEGIN
                        SET @IsMember = 1;
                    END

                    SELECT @IsMember AS IsMember;

                    IF @IsMember = 0
                    BEGIN
                        RETURN;
                    END

                    SELECT TOP (@PageSize)
                        MessageId AS MessageID,
                        ConversationId AS ConversationID,
                        SenderUserId AS SenderUserID,
                        Body AS Body,
                        AttachmentUrl AS AttachmentUrl,
                        SentAt AS SentAt,
                        IsDeleted AS IsDeleted
                    FROM dbo.MessagingMessages
                    WHERE ConversationId = @ConversationID
                      AND (@Before IS NULL OR SentAt < @Before)
                    ORDER BY SentAt DESC, MessageId DESC;
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.sp_MessageConversation_MarkRead
                    @ConversationID INT,
                    @UserID NVARCHAR(450)
                AS
                BEGIN
                    SET NOCOUNT ON;

                    UPDATE dbo.MessagingConversations
                    SET BuyerLastReadAt = CASE WHEN BuyerUserId = @UserID THEN SYSUTCDATETIME() ELSE BuyerLastReadAt END,
                        SellerLastReadAt = CASE WHEN SellerUserId = @UserID THEN SYSUTCDATETIME() ELSE SellerLastReadAt END,
                        UpdatedAt = SYSUTCDATETIME()
                    WHERE ConversationId = @ConversationID
                      AND (BuyerUserId = @UserID OR SellerUserId = @UserID);

                    SELECT CAST(CASE WHEN @@ROWCOUNT > 0 THEN 1 ELSE 0 END AS BIT) AS Updated;
                END;
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.sp_Message_SoftDelete
                    @MessageID BIGINT,
                    @UserID NVARCHAR(450)
                AS
                BEGIN
                    SET NOCOUNT ON;

                    UPDATE dbo.MessagingMessages
                    SET IsDeleted = 1
                    WHERE MessageId = @MessageID
                      AND SenderUserId = @UserID
                      AND IsDeleted = 0;

                    SELECT CAST(CASE WHEN @@ROWCOUNT > 0 THEN 1 ELSE 0 END AS BIT) AS Deleted;
                END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_Message_SoftDelete;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_MessageConversation_MarkRead;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_Message_List;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_Message_Send;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_MessageConversation_ListByUser;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_MessageConversation_CreateOrGet_Order;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_MessageConversation_CreateOrGet_General;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.sp_MessageConversation_GetById;");

            migrationBuilder.DropTable(
                name: "MessagingMessages");

            migrationBuilder.DropTable(
                name: "MessagingConversations");
        }
    }
}

