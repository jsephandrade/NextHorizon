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
            await EnsureUserRowsAsync(connection, buyerUserId, sellerUserId, cancellationToken);
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
            await EnsureUserRowsAsync(connection, buyerUserId, sellerUserId, cancellationToken);
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
            await EnsureUserRowsAsync(connection, senderUserId, null, cancellationToken);

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
                    INSERT INTO dbo.Consumers (consumer_id) VALUES (@BuyerUserID);
            END;

            IF OBJECT_ID(N'[dbo].[Sellers]', N'U') IS NOT NULL
            BEGIN
                IF NOT EXISTS (SELECT 1 FROM dbo.Sellers WHERE seller_id = @SellerUserID)
                    INSERT INTO dbo.Sellers (seller_id) VALUES (@SellerUserID);
            END;
            """;
        command.CommandType = CommandType.Text;

        AddParameter(command, "@BuyerUserID", buyerUserId, DbType.Int32);
        AddParameter(command, "@SellerUserID", sellerUserId, DbType.Int32);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task EnsureUserRowsAsync(DbConnection connection, int firstUserId, int? secondUserId, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            IF OBJECT_ID(N'[dbo].[Users]', N'U') IS NOT NULL
            BEGIN
                DECLARE @UserIds TABLE (user_id INT NOT NULL PRIMARY KEY);
                INSERT INTO @UserIds (user_id)
                SELECT @FirstUserID
                WHERE @FirstUserID IS NOT NULL AND @FirstUserID > 0;

                INSERT INTO @UserIds (user_id)
                SELECT @SecondUserID
                WHERE @SecondUserID IS NOT NULL
                  AND @SecondUserID > 0
                  AND NOT EXISTS (SELECT 1 FROM @UserIds x WHERE x.user_id = @SecondUserID);

                IF EXISTS (SELECT 1 FROM @UserIds)
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

                            INSERT INTO dbo.Users (user_id)
                            SELECT src.user_id
                            FROM @UserIds src
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
                        INSERT INTO dbo.Users (user_id)
                        SELECT src.user_id
                        FROM @UserIds src
                        WHERE NOT EXISTS (SELECT 1 FROM dbo.Users target WHERE target.user_id = src.user_id);
                    END
                END
            END;
            """;
        command.CommandType = CommandType.Text;

        AddParameter(command, "@FirstUserID", firstUserId, DbType.Int32);
        AddParameter(command, "@SecondUserID", secondUserId, DbType.Int32);

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
