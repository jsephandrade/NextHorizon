using System.Data.Common;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using NextHorizon.Data;
using NextHorizon.Models;

namespace NextHorizon.Services;

public sealed class NotificationService : INotificationService
{
    private const string NotificationRecipientTypeUser = "User";
    private const string QaEvaluationNotificationCategory = "QaEvaluation";
    private static readonly Regex QaEvaluationVersionRegex = new(@"version\s+(?<version>\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly ApplicationDbContext _dbContext;

    public NotificationService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<NotificationListData> GetNotificationsAsync(
        int userId,
        int take,
        CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            return new NotificationListData([], false);
        }

        var safeTake = take <= 0 ? 10 : take;
        var connection = _dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            var items = await LoadNotificationsAsync(connection, userId, safeTake, cancellationToken);
            items = await HydrateQaEvaluationTargetsAsync(connection, items, cancellationToken);
            var hasUnread = await HasUnreadNotificationsAsync(connection, userId, cancellationToken);
            return new NotificationListData(items, hasUnread);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    public async Task MarkAllReadAsync(int userId, CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            return;
        }

        var connection = _dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                UPDATE dbo.Notifications
                SET IsRead = 1
                WHERE RecipientType = @RecipientType
                  AND RecipientId = @RecipientId
                  AND IsRead = 0
                """;

            AddParameter(command, "@RecipientType", NotificationRecipientTypeUser);
            AddParameter(command, "@RecipientId", userId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    public async Task ClearAllAsync(int userId, CancellationToken cancellationToken)
    {
        if (userId <= 0)
        {
            return;
        }

        var connection = _dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                DELETE FROM dbo.Notifications
                WHERE RecipientType = @RecipientType
                  AND RecipientId = @RecipientId
                """;

            AddParameter(command, "@RecipientType", NotificationRecipientTypeUser);
            AddParameter(command, "@RecipientId", userId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    public async Task NotifyUserAsync(
        int userId,
        string message,
        string category,
        int? orderId,
        CancellationToken cancellationToken)
    {
        if (userId <= 0 || string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(category))
        {
            return;
        }

        var connection = _dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await InsertNotificationsAsync(connection, [userId], message.Trim(), category.Trim(), orderId, cancellationToken);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    public async Task NotifyUsersByUserTypeAsync(
        string userType,
        string message,
        string category,
        int? orderId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userType) || string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(category))
        {
            return;
        }

        var connection = _dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            var recipientIds = await LoadActiveUserIdsByUserTypeAsync(connection, userType.Trim(), cancellationToken);
            if (recipientIds.Count == 0)
            {
                return;
            }

            await InsertNotificationsAsync(connection, recipientIds, message.Trim(), category.Trim(), orderId, cancellationToken);
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task<List<int>> LoadActiveUserIdsByUserTypeAsync(
        DbConnection connection,
        string userType,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT DISTINCT u.user_id
            FROM staff_info s
            INNER JOIN users u ON s.user_id = u.user_id
            WHERE u.is_active = 1
              AND s.revoked_at IS NULL
              AND u.user_id IS NOT NULL
              AND u.user_type = @UserType
            """;

        AddParameter(command, "@UserType", userType);

        var userIds = new List<int>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            if (reader.IsDBNull(0))
            {
                continue;
            }

            var userId = reader.GetInt32(0);
            if (userId > 0)
            {
                userIds.Add(userId);
            }
        }

        return userIds;
    }

    private static async Task<List<NotificationListItem>> LoadNotificationsAsync(
        DbConnection connection,
        int userId,
        int take,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP (@Take)
                NotificationId,
                Message,
                category,
                CreatedAt,
                IsRead,
                OrderId
            FROM dbo.Notifications
            WHERE RecipientType = @RecipientType
              AND RecipientId = @RecipientId
            ORDER BY CreatedAt DESC
            """;

        AddParameter(command, "@Take", take);
        AddParameter(command, "@RecipientType", NotificationRecipientTypeUser);
        AddParameter(command, "@RecipientId", userId);

        var items = new List<NotificationListItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var notificationId = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
            var message = reader.IsDBNull(1) ? "Notification" : reader.GetString(1);
            var category = reader.IsDBNull(2) ? "General" : reader.GetString(2);
            var createdAt = reader.IsDBNull(3) ? DateTime.UtcNow : reader.GetDateTime(3);
            var isRead = !reader.IsDBNull(4) && reader.GetBoolean(4);
            int? targetId = reader.IsDBNull(5) ? null : reader.GetInt32(5);
            var isClickable = string.Equals(category, "QaEvaluation", StringComparison.OrdinalIgnoreCase)
                && targetId.HasValue
                && targetId.Value > 0;

            items.Add(new NotificationListItem(
                notificationId,
                string.IsNullOrWhiteSpace(message) ? "Notification" : message,
                string.IsNullOrWhiteSpace(category) ? "General" : category,
                createdAt.ToLocalTime().ToString("MMM dd, yyyy hh:mm tt"),
                isRead,
                targetId,
                isClickable));
        }

        return items;
    }

    private static async Task<List<NotificationListItem>> HydrateQaEvaluationTargetsAsync(
        DbConnection connection,
        List<NotificationListItem> items,
        CancellationToken cancellationToken)
    {
        if (items.Count == 0)
        {
            return items;
        }

        var missingVersions = items
            .Where(item =>
                string.Equals(item.Category, QaEvaluationNotificationCategory, StringComparison.OrdinalIgnoreCase)
                && !item.TargetId.HasValue)
            .Select(item => TryExtractQaEvaluationVersion(item.Message))
            .Where(version => version.HasValue)
            .Select(version => version!.Value)
            .Distinct()
            .ToList();

        if (missingVersions.Count == 0)
        {
            return items;
        }

        var templateIdsByVersion = await LoadQaEvaluationTemplateIdsByVersionAsync(connection, missingVersions, cancellationToken);
        if (templateIdsByVersion.Count == 0)
        {
            return items;
        }

        return items
            .Select(item =>
            {
                if (!string.Equals(item.Category, QaEvaluationNotificationCategory, StringComparison.OrdinalIgnoreCase)
                    || item.TargetId.HasValue)
                {
                    return item;
                }

                var versionNumber = TryExtractQaEvaluationVersion(item.Message);
                if (!versionNumber.HasValue || !templateIdsByVersion.TryGetValue(versionNumber.Value, out var templateId))
                {
                    return item;
                }

                return item with
                {
                    TargetId = templateId,
                    IsClickable = templateId > 0
                };
            })
            .ToList();
    }

    private static int? TryExtractQaEvaluationVersion(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        var match = QaEvaluationVersionRegex.Match(message);
        if (!match.Success)
        {
            return null;
        }

        return int.TryParse(match.Groups["version"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var versionNumber)
            ? versionNumber
            : null;
    }

    private static async Task<Dictionary<int, int>> LoadQaEvaluationTemplateIdsByVersionAsync(
        DbConnection connection,
        IReadOnlyCollection<int> versionNumbers,
        CancellationToken cancellationToken)
    {
        var templateIdsByVersion = new Dictionary<int, int>();

        foreach (var versionNumber in versionNumbers)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT TOP (1) QaEvaluationTemplateId
                FROM dbo.QaEvaluationTemplates
                WHERE VersionNumber = @VersionNumber
                ORDER BY QaEvaluationTemplateId DESC
                """;

            AddParameter(command, "@VersionNumber", versionNumber);
            var result = await command.ExecuteScalarAsync(cancellationToken);
            if (result is null || result == DBNull.Value)
            {
                continue;
            }

            var templateId = Convert.ToInt32(result, CultureInfo.InvariantCulture);
            if (templateId > 0)
            {
                templateIdsByVersion[versionNumber] = templateId;
            }
        }

        return templateIdsByVersion;
    }

    private static async Task<bool> HasUnreadNotificationsAsync(
        DbConnection connection,
        int userId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT CASE
                WHEN EXISTS (
                    SELECT 1
                    FROM dbo.Notifications
                    WHERE RecipientType = @RecipientType
                      AND RecipientId = @RecipientId
                      AND IsRead = 0)
                THEN CAST(1 AS bit)
                ELSE CAST(0 AS bit)
            END
            """;

        AddParameter(command, "@RecipientType", NotificationRecipientTypeUser);
        AddParameter(command, "@RecipientId", userId);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is bool value && value;
    }

    private static async Task InsertNotificationsAsync(
        DbConnection connection,
        IReadOnlyCollection<int> recipientIds,
        string message,
        string category,
        int? orderId,
        CancellationToken cancellationToken)
    {
        foreach (var recipientId in recipientIds)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO dbo.Notifications (RecipientType, RecipientId, OrderId, Message, IsRead, CreatedAt, category)
                VALUES (@RecipientType, @RecipientId, @OrderId, @Message, @IsRead, @CreatedAt, @Category)
                """;

            AddParameter(command, "@RecipientType", NotificationRecipientTypeUser);
            AddParameter(command, "@RecipientId", recipientId);
            AddParameter(command, "@OrderId", orderId ?? (object)DBNull.Value);
            AddParameter(command, "@Message", message);
            AddParameter(command, "@IsRead", false);
            AddParameter(command, "@CreatedAt", DateTime.UtcNow);
            AddParameter(command, "@Category", category);

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
