using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MemberTracker.Migrations
{
    /// <inheritdoc />
    public partial class CutoverToSharedUsersGuidKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.Users
                    (
                        user_id INT NOT NULL,
                        CONSTRAINT PK_Users_user_id PRIMARY KEY (user_id)
                    );
                END;

                DECLARE @UsersHasIdentity BIT = 0;
                SELECT @UsersHasIdentity =
                    CASE
                        WHEN EXISTS
                        (
                            SELECT 1
                            FROM sys.columns c
                            WHERE c.object_id = OBJECT_ID(N'[dbo].[Users]', N'U')
                              AND c.name = N'user_id'
                              AND c.is_identity = 1
                        ) THEN 1
                        ELSE 0
                    END;

                IF OBJECT_ID(N'[dbo].[User]', N'U') IS NOT NULL
                   AND EXISTS (
                        SELECT 1
                        FROM sys.columns
                        WHERE object_id = OBJECT_ID(N'[dbo].[User]', N'U')
                          AND name = N'user_id'
                   )
                BEGIN
                    IF @UsersHasIdentity = 1
                        SET IDENTITY_INSERT dbo.Users ON;

                    BEGIN TRY
                        INSERT INTO dbo.Users (user_id)
                        SELECT source.user_id
                        FROM dbo.[User] source
                        WHERE source.user_id IS NOT NULL
                          AND NOT EXISTS
                          (
                              SELECT 1
                              FROM dbo.Users target
                              WHERE target.user_id = source.user_id
                          );
                    END TRY
                    BEGIN CATCH
                        IF @UsersHasIdentity = 1
                            SET IDENTITY_INSERT dbo.Users OFF;

                        THROW;
                    END CATCH;

                    IF @UsersHasIdentity = 1
                        SET IDENTITY_INSERT dbo.Users OFF;
                END;

                IF NOT EXISTS (
                    SELECT 1
                    FROM sys.columns
                    WHERE object_id = OBJECT_ID(N'[dbo].[Users]', N'U')
                      AND name = N'user_id'
                )
                    THROW 51005, 'Migration blocked: required column [user_id] was not found on [dbo].[Users].', 1;
                """);

            migrationBuilder.Sql(
                """
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MemberUploads_AspNetUsers_UserId')
                    ALTER TABLE dbo.MemberUploads DROP CONSTRAINT FK_MemberUploads_AspNetUsers_UserId;

                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MessagingConversations_AspNetUsers_BuyerUserId')
                    ALTER TABLE dbo.MessagingConversations DROP CONSTRAINT FK_MessagingConversations_AspNetUsers_BuyerUserId;

                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MessagingConversations_AspNetUsers_SellerUserId')
                    ALTER TABLE dbo.MessagingConversations DROP CONSTRAINT FK_MessagingConversations_AspNetUsers_SellerUserId;

                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MessagingMessages_AspNetUsers_SenderUserId')
                    ALTER TABLE dbo.MessagingMessages DROP CONSTRAINT FK_MessagingMessages_AspNetUsers_SenderUserId;

                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MemberUploads_User_UserId')
                    ALTER TABLE dbo.MemberUploads DROP CONSTRAINT FK_MemberUploads_User_UserId;

                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MemberUploads_Users_UserId')
                    ALTER TABLE dbo.MemberUploads DROP CONSTRAINT FK_MemberUploads_Users_UserId;

                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MessagingConversations_User_BuyerUserId')
                    ALTER TABLE dbo.MessagingConversations DROP CONSTRAINT FK_MessagingConversations_User_BuyerUserId;

                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MessagingConversations_Users_BuyerUserId')
                    ALTER TABLE dbo.MessagingConversations DROP CONSTRAINT FK_MessagingConversations_Users_BuyerUserId;

                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MessagingConversations_User_SellerUserId')
                    ALTER TABLE dbo.MessagingConversations DROP CONSTRAINT FK_MessagingConversations_User_SellerUserId;

                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MessagingConversations_Users_SellerUserId')
                    ALTER TABLE dbo.MessagingConversations DROP CONSTRAINT FK_MessagingConversations_Users_SellerUserId;

                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MessagingMessages_User_SenderUserId')
                    ALTER TABLE dbo.MessagingMessages DROP CONSTRAINT FK_MessagingMessages_User_SenderUserId;

                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_MessagingMessages_Users_SenderUserId')
                    ALTER TABLE dbo.MessagingMessages DROP CONSTRAINT FK_MessagingMessages_Users_SenderUserId;
                """);

            migrationBuilder.Sql(
                """
                DECLARE @UsersHasIdentity BIT = 0;
                SELECT @UsersHasIdentity =
                    CASE
                        WHEN EXISTS
                        (
                            SELECT 1
                            FROM sys.columns c
                            WHERE c.object_id = OBJECT_ID(N'[dbo].[Users]', N'U')
                              AND c.name = N'user_id'
                              AND c.is_identity = 1
                        ) THEN 1
                        ELSE 0
                    END;

                IF @UsersHasIdentity = 1
                    SET IDENTITY_INSERT dbo.Users ON;

                BEGIN TRY
                    INSERT INTO dbo.Users (user_id)
                    SELECT candidate.user_id
                    FROM
                    (
                        SELECT DISTINCT TRY_CONVERT(int, mu.UserId) AS user_id
                        FROM dbo.MemberUploads mu
                        UNION
                        SELECT DISTINCT TRY_CONVERT(int, mc.BuyerUserId) AS user_id
                        FROM dbo.MessagingConversations mc
                        UNION
                        SELECT DISTINCT TRY_CONVERT(int, mc.SellerUserId) AS user_id
                        FROM dbo.MessagingConversations mc
                        UNION
                        SELECT DISTINCT TRY_CONVERT(int, mm.SenderUserId) AS user_id
                        FROM dbo.MessagingMessages mm
                    ) candidate
                    WHERE candidate.user_id IS NOT NULL
                      AND candidate.user_id > 0
                      AND NOT EXISTS
                      (
                          SELECT 1
                          FROM dbo.Users target
                          WHERE target.user_id = candidate.user_id
                      );
                END TRY
                BEGIN CATCH
                    IF @UsersHasIdentity = 1
                        SET IDENTITY_INSERT dbo.Users OFF;

                    THROW;
                END CATCH;

                IF @UsersHasIdentity = 1
                    SET IDENTITY_INSERT dbo.Users OFF;
                """);

            migrationBuilder.Sql(
                """
                DELETE mm
                FROM dbo.MessagingMessages mm
                WHERE TRY_CONVERT(int, mm.SenderUserId) IS NULL;

                DELETE mc
                FROM dbo.MessagingConversations mc
                WHERE TRY_CONVERT(int, mc.BuyerUserId) IS NULL
                   OR TRY_CONVERT(int, mc.SellerUserId) IS NULL;

                DELETE mm
                FROM dbo.MessagingMessages mm
                LEFT JOIN dbo.MessagingConversations mc ON mc.ConversationId = mm.ConversationId
                WHERE mc.ConversationId IS NULL;

                DELETE mu
                FROM dbo.MemberUploads mu
                WHERE TRY_CONVERT(int, mu.UserId) IS NULL;
                """);

            migrationBuilder.Sql(
                """
                DELETE mm
                FROM dbo.MessagingMessages mm
                LEFT JOIN dbo.Users u ON u.user_id = TRY_CONVERT(int, mm.SenderUserId)
                WHERE u.user_id IS NULL;

                DELETE mc
                FROM dbo.MessagingConversations mc
                LEFT JOIN dbo.Users buyer ON buyer.user_id = TRY_CONVERT(int, mc.BuyerUserId)
                LEFT JOIN dbo.Users seller ON seller.user_id = TRY_CONVERT(int, mc.SellerUserId)
                WHERE buyer.user_id IS NULL OR seller.user_id IS NULL;

                DELETE mm
                FROM dbo.MessagingMessages mm
                LEFT JOIN dbo.MessagingConversations mc ON mc.ConversationId = mm.ConversationId
                WHERE mc.ConversationId IS NULL;

                DELETE mu
                FROM dbo.MemberUploads mu
                LEFT JOIN dbo.Users u ON u.user_id = TRY_CONVERT(int, mu.UserId)
                WHERE u.user_id IS NULL;
                """);

            migrationBuilder.AlterColumn<int>(
                name: "SenderUserId",
                table: "MessagingMessages",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<int>(
                name: "SellerUserId",
                table: "MessagingConversations",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<int>(
                name: "BuyerUserId",
                table: "MessagingConversations",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "MemberUploads",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.Sql(
                """
                ALTER TABLE dbo.MemberUploads WITH CHECK
                    ADD CONSTRAINT FK_MemberUploads_User_UserId
                    FOREIGN KEY (UserId) REFERENCES dbo.Users(user_id) ON DELETE NO ACTION;

                ALTER TABLE dbo.MessagingConversations WITH CHECK
                    ADD CONSTRAINT FK_MessagingConversations_User_BuyerUserId
                    FOREIGN KEY (BuyerUserId) REFERENCES dbo.Users(user_id) ON DELETE NO ACTION;

                ALTER TABLE dbo.MessagingConversations WITH CHECK
                    ADD CONSTRAINT FK_MessagingConversations_User_SellerUserId
                    FOREIGN KEY (SellerUserId) REFERENCES dbo.Users(user_id) ON DELETE NO ACTION;

                ALTER TABLE dbo.MessagingMessages WITH CHECK
                    ADD CONSTRAINT FK_MessagingMessages_User_SenderUserId
                    FOREIGN KEY (SenderUserId) REFERENCES dbo.Users(user_id) ON DELETE NO ACTION;
                """);

            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS dbo.AspNetRoleClaims;
                DROP TABLE IF EXISTS dbo.AspNetUserClaims;
                DROP TABLE IF EXISTS dbo.AspNetUserLogins;
                DROP TABLE IF EXISTS dbo.AspNetUserRoles;
                DROP TABLE IF EXISTS dbo.AspNetUserTokens;
                DROP TABLE IF EXISTS dbo.AspNetRoles;
                DROP TABLE IF EXISTS dbo.AspNetUsers;
                """);

            migrationBuilder.Sql(
                """
                DECLARE @Procedures TABLE (ProcName SYSNAME);
                INSERT INTO @Procedures (ProcName)
                VALUES
                    (N'sp_MemberUpload_Create'),
                    (N'sp_MemberUpload_GetMy'),
                    (N'sp_MemberUpload_Update'),
                    (N'sp_MemberUpload_Delete'),
                    (N'sp_MessageConversation_GetById'),
                    (N'sp_MessageConversation_CreateOrGet_General'),
                    (N'sp_MessageConversation_CreateOrGet_Order'),
                    (N'sp_MessageConversation_ListByUser'),
                    (N'sp_Message_Send'),
                    (N'sp_Message_List'),
                    (N'sp_MessageConversation_MarkRead'),
                    (N'sp_Message_SoftDelete');

                DECLARE @ProcName SYSNAME;
                DECLARE @Definition NVARCHAR(MAX);

                DECLARE ProcCursor CURSOR LOCAL FAST_FORWARD FOR
                    SELECT ProcName FROM @Procedures;

                OPEN ProcCursor;
                FETCH NEXT FROM ProcCursor INTO @ProcName;

                WHILE @@FETCH_STATUS = 0
                BEGIN
                    IF OBJECT_ID(N'dbo.' + @ProcName, N'P') IS NOT NULL
                    BEGIN
                        SELECT @Definition = sm.definition
                        FROM sys.sql_modules sm
                        WHERE sm.object_id = OBJECT_ID(N'dbo.' + @ProcName, N'P');

                        IF @Definition IS NOT NULL
                        BEGIN
                            SET @Definition = REPLACE(@Definition, N'@UserID NVARCHAR(450)', N'@UserID INT');
                            SET @Definition = REPLACE(@Definition, N'@UserId NVARCHAR(450)', N'@UserId INT');
                            SET @Definition = REPLACE(@Definition, N'@BuyerUserID NVARCHAR(450)', N'@BuyerUserID INT');
                            SET @Definition = REPLACE(@Definition, N'@BuyerUserId NVARCHAR(450)', N'@BuyerUserId INT');
                            SET @Definition = REPLACE(@Definition, N'@SellerUserID NVARCHAR(450)', N'@SellerUserID INT');
                            SET @Definition = REPLACE(@Definition, N'@SellerUserId NVARCHAR(450)', N'@SellerUserId INT');
                            SET @Definition = REPLACE(@Definition, N'@SenderUserID NVARCHAR(450)', N'@SenderUserID INT');
                            SET @Definition = REPLACE(@Definition, N'@SenderUserId NVARCHAR(450)', N'@SenderUserId INT');
                            SET @Definition = REPLACE(@Definition, N'@UserID UNIQUEIDENTIFIER', N'@UserID INT');
                            SET @Definition = REPLACE(@Definition, N'@UserId UNIQUEIDENTIFIER', N'@UserId INT');
                            SET @Definition = REPLACE(@Definition, N'@BuyerUserID UNIQUEIDENTIFIER', N'@BuyerUserID INT');
                            SET @Definition = REPLACE(@Definition, N'@BuyerUserId UNIQUEIDENTIFIER', N'@BuyerUserId INT');
                            SET @Definition = REPLACE(@Definition, N'@SellerUserID UNIQUEIDENTIFIER', N'@SellerUserID INT');
                            SET @Definition = REPLACE(@Definition, N'@SellerUserId UNIQUEIDENTIFIER', N'@SellerUserId INT');
                            SET @Definition = REPLACE(@Definition, N'@SenderUserID UNIQUEIDENTIFIER', N'@SenderUserID INT');
                            SET @Definition = REPLACE(@Definition, N'@SenderUserId UNIQUEIDENTIFIER', N'@SenderUserId INT');

                            -- Drop/recreate to avoid create-vs-alter header collisions.
                            DECLARE @DropSql NVARCHAR(400) = N'DROP PROCEDURE IF EXISTS dbo.' + QUOTENAME(@ProcName) + N';';
                            EXEC sp_executesql @DropSql;

                            -- Normalize headers to CREATE PROCEDURE after drop.
                            SET @Definition = REPLACE(@Definition, N'CREATE OR ALTER PROCEDURE', N'CREATE PROCEDURE');
                            SET @Definition = REPLACE(@Definition, N'create or alter procedure', N'CREATE PROCEDURE');
                            SET @Definition = REPLACE(@Definition, N'Create Or Alter Procedure', N'CREATE PROCEDURE');
                            SET @Definition = REPLACE(@Definition, N'ALTER PROCEDURE', N'CREATE PROCEDURE');
                            SET @Definition = REPLACE(@Definition, N'alter procedure', N'CREATE PROCEDURE');
                            SET @Definition = REPLACE(@Definition, N'Alter Procedure', N'CREATE PROCEDURE');
                            SET @Definition = REPLACE(@Definition, N'CREATE PROC', N'CREATE PROCEDURE');
                            SET @Definition = REPLACE(@Definition, N'create proc', N'CREATE PROCEDURE');
                            SET @Definition = REPLACE(@Definition, N'Create Proc', N'CREATE PROCEDURE');

                            EXEC sp_executesql @Definition;
                        END
                    END

                    FETCH NEXT FROM ProcCursor INTO @ProcName;
                END

                CLOSE ProcCursor;
                DEALLOCATE ProcCursor;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MemberUploads_User_UserId",
                table: "MemberUploads");

            migrationBuilder.DropForeignKey(
                name: "FK_MessagingConversations_User_BuyerUserId",
                table: "MessagingConversations");

            migrationBuilder.DropForeignKey(
                name: "FK_MessagingConversations_User_SellerUserId",
                table: "MessagingConversations");

            migrationBuilder.DropForeignKey(
                name: "FK_MessagingMessages_User_SenderUserId",
                table: "MessagingMessages");

            migrationBuilder.AlterColumn<string>(
                name: "SenderUserId",
                table: "MessagingMessages",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "SellerUserId",
                table: "MessagingConversations",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "BuyerUserId",
                table: "MessagingConversations",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "MemberUploads",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_MemberUploads_AspNetUsers_UserId",
                table: "MemberUploads",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MessagingConversations_AspNetUsers_BuyerUserId",
                table: "MessagingConversations",
                column: "BuyerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MessagingConversations_AspNetUsers_SellerUserId",
                table: "MessagingConversations",
                column: "SellerUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MessagingMessages_AspNetUsers_SenderUserId",
                table: "MessagingMessages",
                column: "SenderUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                """
                DECLARE @Procedures TABLE (ProcName SYSNAME);
                INSERT INTO @Procedures (ProcName)
                VALUES
                    (N'sp_MemberUpload_Create'),
                    (N'sp_MemberUpload_GetMy'),
                    (N'sp_MemberUpload_Update'),
                    (N'sp_MemberUpload_Delete'),
                    (N'sp_MessageConversation_GetById'),
                    (N'sp_MessageConversation_CreateOrGet_General'),
                    (N'sp_MessageConversation_CreateOrGet_Order'),
                    (N'sp_MessageConversation_ListByUser'),
                    (N'sp_Message_Send'),
                    (N'sp_Message_List'),
                    (N'sp_MessageConversation_MarkRead'),
                    (N'sp_Message_SoftDelete');

                DECLARE @ProcName SYSNAME;
                DECLARE @Definition NVARCHAR(MAX);

                DECLARE ProcCursor CURSOR LOCAL FAST_FORWARD FOR
                    SELECT ProcName FROM @Procedures;

                OPEN ProcCursor;
                FETCH NEXT FROM ProcCursor INTO @ProcName;

                WHILE @@FETCH_STATUS = 0
                BEGIN
                    IF OBJECT_ID(N'dbo.' + @ProcName, N'P') IS NOT NULL
                    BEGIN
                        SELECT @Definition = sm.definition
                        FROM sys.sql_modules sm
                        WHERE sm.object_id = OBJECT_ID(N'dbo.' + @ProcName, N'P');

                        IF @Definition IS NOT NULL
                        BEGIN
                            SET @Definition = REPLACE(@Definition, N'@UserID INT', N'@UserID NVARCHAR(450)');
                            SET @Definition = REPLACE(@Definition, N'@UserId INT', N'@UserId NVARCHAR(450)');
                            SET @Definition = REPLACE(@Definition, N'@BuyerUserID INT', N'@BuyerUserID NVARCHAR(450)');
                            SET @Definition = REPLACE(@Definition, N'@BuyerUserId INT', N'@BuyerUserId NVARCHAR(450)');
                            SET @Definition = REPLACE(@Definition, N'@SellerUserID INT', N'@SellerUserID NVARCHAR(450)');
                            SET @Definition = REPLACE(@Definition, N'@SellerUserId INT', N'@SellerUserId NVARCHAR(450)');
                            SET @Definition = REPLACE(@Definition, N'@SenderUserID INT', N'@SenderUserID NVARCHAR(450)');
                            SET @Definition = REPLACE(@Definition, N'@SenderUserId INT', N'@SenderUserId NVARCHAR(450)');

                            -- Drop/recreate to avoid create-vs-alter header collisions.
                            DECLARE @DropSql NVARCHAR(400) = N'DROP PROCEDURE IF EXISTS dbo.' + QUOTENAME(@ProcName) + N';';
                            EXEC sp_executesql @DropSql;

                            -- Normalize headers to CREATE PROCEDURE after drop.
                            SET @Definition = REPLACE(@Definition, N'CREATE OR ALTER PROCEDURE', N'CREATE PROCEDURE');
                            SET @Definition = REPLACE(@Definition, N'create or alter procedure', N'CREATE PROCEDURE');
                            SET @Definition = REPLACE(@Definition, N'Create Or Alter Procedure', N'CREATE PROCEDURE');
                            SET @Definition = REPLACE(@Definition, N'ALTER PROCEDURE', N'CREATE PROCEDURE');
                            SET @Definition = REPLACE(@Definition, N'alter procedure', N'CREATE PROCEDURE');
                            SET @Definition = REPLACE(@Definition, N'Alter Procedure', N'CREATE PROCEDURE');
                            SET @Definition = REPLACE(@Definition, N'CREATE PROC', N'CREATE PROCEDURE');
                            SET @Definition = REPLACE(@Definition, N'create proc', N'CREATE PROCEDURE');
                            SET @Definition = REPLACE(@Definition, N'Create Proc', N'CREATE PROCEDURE');

                            EXEC sp_executesql @Definition;
                        END
                    END

                    FETCH NEXT FROM ProcCursor INTO @ProcName;
                END

                CLOSE ProcCursor;
                DEALLOCATE ProcCursor;
                """);
        }
    }
}
