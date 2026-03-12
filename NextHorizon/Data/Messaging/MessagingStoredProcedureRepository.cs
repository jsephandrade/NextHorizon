using System.Data;
using System.Data.Common;
using MemberTracker.Models;
using MemberTracker.Models.Messaging;
using Microsoft.EntityFrameworkCore;

namespace MemberTracker.Data.Messaging;

public sealed class MessagingStoredProcedureRepository : IMessagingRepository
{
    private readonly ApplicationDbContext _dbContext;

    public MessagingStoredProcedureRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<MessageConversationSummary> CreateOrGetGeneralAsync(int buyerUserId, int sellerUserId, CancellationToken cancellationToken)
        => WithOpenConnectionAsync(async connection =>
        {
            await EnsureUserRowsAsync(
                connection,
                (buyerUserId, "Consumer"),
                (sellerUserId, "Seller"),
                cancellationToken);
            await EnsureParticipantRowsAsync(connection, buyerUserId, sellerUserId, cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.sp_MessageConversation_CreateOrGet_General";
            command.CommandType = CommandType.StoredProcedure;

            AddParameter(command, "@BuyerUserID", buyerUserId, DbType.Int32);
            AddParameter(command, "@SellerUserID", sellerUserId, DbType.Int32);

            var conversation = await ReadSingleConversationAsync(command, cancellationToken);
            return conversation ?? throw new InvalidOperationException("Stored procedure sp_MessageConversation_CreateOrGet_General did not return a conversation.");
        }, cancellationToken);

    public Task<MessageConversationSummary> CreateOrGetOrderAsync(int orderId, int buyerUserId, int sellerUserId, CancellationToken cancellationToken)
        => WithOpenConnectionAsync(async connection =>
        {
            await EnsureUserRowsAsync(
                connection,
                (buyerUserId, "Consumer"),
                (sellerUserId, "Seller"),
                cancellationToken);
            await EnsureParticipantRowsAsync(connection, buyerUserId, sellerUserId, cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.sp_MessageConversation_CreateOrGet_Order";
            command.CommandType = CommandType.StoredProcedure;

            AddParameter(command, "@OrderID", orderId, DbType.Int32);
            AddParameter(command, "@BuyerUserID", buyerUserId, DbType.Int32);
            AddParameter(command, "@SellerUserID", sellerUserId, DbType.Int32);

            var conversation = await ReadSingleConversationAsync(command, cancellationToken);
            return conversation ?? throw new InvalidOperationException("Stored procedure sp_MessageConversation_CreateOrGet_Order did not return a conversation.");
        }, cancellationToken);

    public Task<PagedResult<MessageConversationSummary>> ListByUserAsync(int userId, int pageNumber, int pageSize, CancellationToken cancellationToken)
        => WithOpenConnectionAsync(async connection =>
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.sp_MessageConversation_ListByUser";
            command.CommandType = CommandType.StoredProcedure;

            AddParameter(command, "@UserID", userId, DbType.Int32);
            AddParameter(command, "@Page", pageNumber, DbType.Int32);
            AddParameter(command, "@PageSize", pageSize, DbType.Int32);

            var items = new List<MessageConversationSummary>();

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(MapConversation(reader));
            }

            var totalCount = 0;
            if (await reader.NextResultAsync(cancellationToken) && await reader.ReadAsync(cancellationToken))
            {
                totalCount = reader.GetInt32(reader.GetOrdinal("TotalCount"));
            }

            return new PagedResult<MessageConversationSummary>
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                Items = items,
            };
        }, cancellationToken);

    public Task<MessageConversationSummary?> GetConversationAsync(int conversationId, int userId, CancellationToken cancellationToken)
        => WithOpenConnectionAsync(async connection =>
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.sp_MessageConversation_GetById";
            command.CommandType = CommandType.StoredProcedure;

            AddParameter(command, "@ConversationID", conversationId, DbType.Int32);
            AddParameter(command, "@UserID", userId, DbType.Int32);

            return await ReadSingleConversationAsync(command, cancellationToken);
        }, cancellationToken);

    public Task<MessageItem?> SendMessageAsync(int conversationId, int senderUserId, string body, string? attachmentUrl, CancellationToken cancellationToken)
        => WithOpenConnectionAsync(async connection =>
        {
            await EnsureUserRowsAsync(
                connection,
                (senderUserId, "Messaging"),
                null,
                cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.sp_Message_Send";
            command.CommandType = CommandType.StoredProcedure;

            AddParameter(command, "@ConversationID", conversationId, DbType.Int32);
            AddParameter(command, "@SenderUserID", senderUserId, DbType.Int32);
            AddParameter(command, "@Body", body, DbType.String);
            AddParameter(command, "@AttachmentUrl", attachmentUrl, DbType.String);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            return MapMessage(reader);
        }, cancellationToken);

    public Task<IReadOnlyList<MessageItem>?> ListMessagesAsync(int conversationId, int userId, DateTime? before, int pageSize, CancellationToken cancellationToken)
        => WithOpenConnectionAsync(async connection =>
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.sp_Message_List";
            command.CommandType = CommandType.StoredProcedure;

            AddParameter(command, "@ConversationID", conversationId, DbType.Int32);
            AddParameter(command, "@UserID", userId, DbType.Int32);
            AddParameter(command, "@Before", before, DbType.DateTime2);
            AddParameter(command, "@PageSize", pageSize, DbType.Int32);

            var items = new List<MessageItem>();

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.NextResultAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    items.Add(MapMessage(reader));
                }

                return (IReadOnlyList<MessageItem>)items;
            }

            return (IReadOnlyList<MessageItem>?)null;
        }, cancellationToken);

    public Task<bool> MarkReadAsync(int conversationId, int userId, CancellationToken cancellationToken)
        => WithOpenConnectionAsync(async connection =>
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.sp_MessageConversation_MarkRead";
            command.CommandType = CommandType.StoredProcedure;

            AddParameter(command, "@ConversationID", conversationId, DbType.Int32);
            AddParameter(command, "@UserID", userId, DbType.Int32);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return false;
            }

            return reader.GetBoolean(reader.GetOrdinal("Updated"));
        }, cancellationToken);

    public Task<bool> SoftDeleteMessageAsync(long messageId, int userId, CancellationToken cancellationToken)
        => WithOpenConnectionAsync(async connection =>
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "dbo.sp_Message_SoftDelete";
            command.CommandType = CommandType.StoredProcedure;

            AddParameter(command, "@MessageID", messageId, DbType.Int64);
            AddParameter(command, "@UserID", userId, DbType.Int32);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return false;
            }

            return reader.GetBoolean(reader.GetOrdinal("Deleted"));
        }, cancellationToken);

    private async Task<MessageConversationSummary?> ReadSingleConversationAsync(DbCommand command, CancellationToken cancellationToken)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return MapConversation(reader);
    }

    private static async Task EnsureParticipantRowsAsync(DbConnection connection, int buyerUserId, int sellerUserId, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            IF OBJECT_ID(N'[dbo].[Consumers]', N'U') IS NOT NULL
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM dbo.Consumers WHERE consumer_id = @BuyerUserID)
                BEGIN
                    IF EXISTS
                    (
                        SELECT 1
                        FROM sys.identity_columns ic
                        WHERE ic.object_id = OBJECT_ID(N'[dbo].[Consumers]', N'U')
                          AND ic.name = N'consumer_id'
                    )
                    BEGIN
                        BEGIN TRY
                            SET IDENTITY_INSERT dbo.Consumers ON;

                            INSERT INTO dbo.Consumers
                            (
                                consumer_id,
                                user_id,
                                first_name,
                                last_name,
                                address,
                                phone_number,
                                created_at,
                                username
                            )
                            VALUES
                            (
                                @BuyerUserID,
                                @BuyerUserID,
                                CONCAT(N'Consumer ', @BuyerUserID),
                                N'Messaging',
                                CONCAT(N'Placeholder address for consumer ', @BuyerUserID),
                                NULL,
                                SYSUTCDATETIME(),
                                CONCAT(N'consumer', @BuyerUserID)
                            );

                            SET IDENTITY_INSERT dbo.Consumers OFF;
                        END TRY
                        BEGIN CATCH
                            BEGIN TRY
                                SET IDENTITY_INSERT dbo.Consumers OFF;
                            END TRY
                            BEGIN CATCH
                            END CATCH;

                            THROW;
                        END CATCH
                    END
                    ELSE
                    BEGIN
                        INSERT INTO dbo.Consumers
                        (
                            user_id,
                            first_name,
                            last_name,
                            address,
                            phone_number,
                            created_at,
                            username
                        )
                        VALUES
                        (
                            @BuyerUserID,
                            CONCAT(N'Consumer ', @BuyerUserID),
                            N'Messaging',
                            CONCAT(N'Placeholder address for consumer ', @BuyerUserID),
                            NULL,
                            SYSUTCDATETIME(),
                            CONCAT(N'consumer', @BuyerUserID)
                        );
                    END
                END
            END;

            IF OBJECT_ID(N'[dbo].[Sellers]', N'U') IS NOT NULL
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM dbo.Sellers WHERE seller_id = @SellerUserID)
                BEGIN
                    IF EXISTS
                    (
                        SELECT 1
                        FROM sys.identity_columns ic
                        WHERE ic.object_id = OBJECT_ID(N'[dbo].[Sellers]', N'U')
                          AND ic.name = N'seller_id'
                    )
                    BEGIN
                        BEGIN TRY
                            SET IDENTITY_INSERT dbo.Sellers ON;

                            INSERT INTO dbo.Sellers
                            (
                                seller_id,
                                user_id,
                                business_type,
                                business_name,
                                business_email,
                                business_phone,
                                tax_id,
                                business_address,
                                logo_path,
                                document_path,
                                seller_status,
                                created_at
                            )
                            VALUES
                            (
                                @SellerUserID,
                                @SellerUserID,
                                N'Demo Storefront',
                                CONCAT(N'Storefront Seller ', @SellerUserID),
                                CONCAT(N'seller', @SellerUserID, N'@placeholder.local'),
                                CONCAT(N'09', RIGHT(CONCAT(N'000000000', @SellerUserID), 9)),
                                NULL,
                                CONCAT(N'Placeholder business address for seller ', @SellerUserID),
                                NULL,
                                NULL,
                                N'active',
                                SYSUTCDATETIME()
                            );

                            SET IDENTITY_INSERT dbo.Sellers OFF;
                        END TRY
                        BEGIN CATCH
                            BEGIN TRY
                                SET IDENTITY_INSERT dbo.Sellers OFF;
                            END TRY
                            BEGIN CATCH
                            END CATCH;

                            THROW;
                        END CATCH
                    END
                    ELSE
                    BEGIN
                        INSERT INTO dbo.Sellers
                        (
                            user_id,
                            business_type,
                            business_name,
                            business_email,
                            business_phone,
                            tax_id,
                            business_address,
                            logo_path,
                            document_path,
                            seller_status,
                            created_at
                        )
                        VALUES
                        (
                            @SellerUserID,
                            N'Demo Storefront',
                            CONCAT(N'Storefront Seller ', @SellerUserID),
                            CONCAT(N'seller', @SellerUserID, N'@placeholder.local'),
                            CONCAT(N'09', RIGHT(CONCAT(N'000000000', @SellerUserID), 9)),
                            NULL,
                            CONCAT(N'Placeholder business address for seller ', @SellerUserID),
                            NULL,
                            NULL,
                            N'active',
                            SYSUTCDATETIME()
                        );
                    END
                END
            END;
            """;
        command.CommandType = CommandType.Text;

        AddParameter(command, "@BuyerUserID", buyerUserId, DbType.Int32);
        AddParameter(command, "@SellerUserID", sellerUserId, DbType.Int32);

        try
        {
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to bootstrap messaging participants for buyerUserId={buyerUserId} and sellerUserId={sellerUserId}.",
                ex);
        }
    }

    private static async Task EnsureUserRowsAsync(
        DbConnection connection,
        (int UserId, string UserType) firstUser,
        (int UserId, string UserType)? secondUser,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
            BEGIN
                DECLARE @UserSeeds TABLE
                (
                    user_id INT NOT NULL PRIMARY KEY,
                    email VARCHAR(255) NOT NULL,
                    password_hash VARCHAR(255) NOT NULL,
                    user_type VARCHAR(20) NULL
                );

                INSERT INTO @UserSeeds (user_id, email, password_hash, user_type)
                SELECT
                    @FirstUserID,
                    CONCAT(LOWER(@FirstUserType), @FirstUserID, '@placeholder.local'),
                    CONCAT('placeholder-hash-', LOWER(@FirstUserType), '-', @FirstUserID),
                    @FirstUserType
                WHERE @FirstUserID > 0;

                INSERT INTO @UserSeeds (user_id, email, password_hash, user_type)
                SELECT
                    @SecondUserID,
                    CONCAT(LOWER(@SecondUserType), @SecondUserID, '@placeholder.local'),
                    CONCAT('placeholder-hash-', LOWER(@SecondUserType), '-', @SecondUserID),
                    @SecondUserType
                WHERE @SecondUserID IS NOT NULL
                  AND @SecondUserID > 0
                  AND NOT EXISTS (SELECT 1 FROM @UserSeeds x WHERE x.user_id = @SecondUserID);

                IF EXISTS (SELECT 1 FROM @UserSeeds)
                BEGIN
                    IF EXISTS
                    (
                        SELECT 1
                        FROM sys.identity_columns ic
                        WHERE ic.object_id = OBJECT_ID(N'[dbo].[Users]', N'U')
                          AND ic.name = N'user_id'
                    )
                    BEGIN
                        BEGIN TRY
                            SET IDENTITY_INSERT dbo.Users ON;

                            INSERT INTO dbo.Users
                            (
                                user_id,
                                email,
                                password_hash,
                                is_active,
                                created_at,
                                updated_at,
                                user_type
                            )
                            SELECT
                                src.user_id,
                                src.email,
                                src.password_hash,
                                1,
                                SYSUTCDATETIME(),
                                SYSUTCDATETIME(),
                                src.user_type
                            FROM @UserSeeds src
                            WHERE NOT EXISTS (SELECT 1 FROM dbo.Users target WHERE target.user_id = src.user_id);

                            SET IDENTITY_INSERT dbo.Users OFF;
                        END TRY
                        BEGIN CATCH
                            BEGIN TRY
                                SET IDENTITY_INSERT dbo.Users OFF;
                            END TRY
                            BEGIN CATCH
                            END CATCH;

                            THROW;
                        END CATCH
                    END
                    ELSE
                    BEGIN
                        INSERT INTO dbo.Users
                        (
                            email,
                            password_hash,
                            is_active,
                            created_at,
                            updated_at,
                            user_type
                        )
                        SELECT
                            src.email,
                            src.password_hash,
                            1,
                            SYSUTCDATETIME(),
                            SYSUTCDATETIME(),
                            src.user_type
                        FROM @UserSeeds src
                        WHERE NOT EXISTS (SELECT 1 FROM dbo.Users target WHERE target.user_id = src.user_id);
                    END
                END
            END;
            """;
        command.CommandType = CommandType.Text;

        AddParameter(command, "@FirstUserID", firstUser.UserId, DbType.Int32);
        AddParameter(command, "@FirstUserType", firstUser.UserType, DbType.String);
        AddParameter(command, "@SecondUserID", secondUser?.UserId, DbType.Int32);
        AddParameter(command, "@SecondUserType", secondUser?.UserType, DbType.String);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static MessageConversationSummary MapConversation(DbDataReader reader)
    {
        return new MessageConversationSummary(
            reader.GetInt32(reader.GetOrdinal("ConversationID")),
            reader.GetInt32(reader.GetOrdinal("BuyerUserID")),
            reader.GetInt32(reader.GetOrdinal("SellerUserID")),
            (ConversationContextType)reader.GetByte(reader.GetOrdinal("ContextType")),
            GetNullableInt(reader, "OrderID"),
            GetNullableDateTime(reader, "LastMessageAt"),
            GetNullableDateTime(reader, "BuyerLastReadAt"),
            GetNullableDateTime(reader, "SellerLastReadAt"),
            GetNullableString(reader, "LastMessagePreview"),
            GetNullableInt(reader, "UnreadCount") ?? 0,
            reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            reader.GetDateTime(reader.GetOrdinal("UpdatedAt")));
    }

    private static MessageItem MapMessage(DbDataReader reader)
    {
        var isDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted"));
        var body = GetNullableString(reader, "Body");

        return new MessageItem(
            reader.GetInt64(reader.GetOrdinal("MessageID")),
            reader.GetInt32(reader.GetOrdinal("ConversationID")),
            reader.GetInt32(reader.GetOrdinal("SenderUserID")),
            isDeleted ? null : body,
            GetNullableString(reader, "AttachmentUrl"),
            reader.GetDateTime(reader.GetOrdinal("SentAt")),
            isDeleted);
    }

    private async Task<T> WithOpenConnectionAsync<T>(Func<DbConnection, Task<T>> action, CancellationToken cancellationToken)
    {
        var connection = _dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            return await action(connection);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static string? GetNullableString(DbDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static int? GetNullableInt(DbDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
    }

    private static DateTime? GetNullableDateTime(DbDataReader reader, string columnName)
    {
        var ordinal = reader.GetOrdinal(columnName);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }

    private static void AddParameter(DbCommand command, string name, object? value, DbType dbType)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = dbType;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }
}
