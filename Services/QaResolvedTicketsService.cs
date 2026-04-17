using System.Data;
using Microsoft.EntityFrameworkCore;
using NextHorizon.Data;
using NextHorizon.Models.HelpCenter;
using NextHorizon.Models.QA;

namespace NextHorizon.Services;

public sealed class QaResolvedTicketsService : IQaResolvedTicketsService
{
    private readonly ApplicationDbContext _dbContext;

    public QaResolvedTicketsService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<QaResolvedTicketsResponse> GetResolvedTicketsAsync(
        string? range,
        DateOnly? from,
        DateOnly? to,
        string? search,
        CancellationToken cancellationToken)
    {
        var (startUtc, endExclusiveUtc) = ResolveDateRange(range, from, to);

        var resolvedFaqQuery = _dbContext.SupportFaqRecords
            .AsNoTracking()
            .Where(item => item.Status == "Resolved" && item.EndTime != null);

        if (startUtc.HasValue)
        {
            resolvedFaqQuery = resolvedFaqQuery.Where(item => item.EndTime >= startUtc.Value);
        }

        if (endExclusiveUtc.HasValue)
        {
            resolvedFaqQuery = resolvedFaqQuery.Where(item => item.EndTime < endExclusiveUtc.Value);
        }

        var resolvedFaqs = await resolvedFaqQuery
            .Select(item => new ResolvedConversationSeed(
                item.Id,
                item.AgentId,
                item.EndTime!.Value))
            .ToListAsync(cancellationToken);

        if (resolvedFaqs.Count == 0)
        {
            return new QaResolvedTicketsResponse(0, 0, 0, Array.Empty<QaResolvedTicketItem>());
        }

        var supportFaqIds = resolvedFaqs.Select(item => item.SupportFaqId).Distinct().ToArray();
        var latestSessions = await _dbContext.LiveAgentSessions
            .AsNoTracking()
            .Where(item => supportFaqIds.Contains(item.SupportFaqId))
            .ToListAsync(cancellationToken);

        var latestSessionsBySupportFaqId = latestSessions
            .GroupBy(item => item.SupportFaqId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(item => item.UpdatedAt)
                    .ThenByDescending(item => item.LiveAgentSessionId)
                    .First());

        var consumerIds = latestSessionsBySupportFaqId.Values
            .Where(item => item.ConsumerId.HasValue)
            .Select(item => item.ConsumerId!.Value)
            .Distinct()
            .ToArray();

        var consumers = consumerIds.Length == 0
            ? new Dictionary<int, ConsumerRef>()
            : await _dbContext.Set<ConsumerRef>()
                .AsNoTracking()
                .Where(item => consumerIds.Contains(item.ConsumerId))
                .ToDictionaryAsync(item => item.ConsumerId, cancellationToken);

        var agentUserIds = resolvedFaqs
            .Where(item => item.AgentUserId.HasValue)
            .Select(item => item.AgentUserId!.Value)
            .Distinct()
            .ToArray();

        var agentNames = await LoadAgentNamesAsync(agentUserIds, cancellationToken);

        var reviews = await _dbContext.QaReviews
            .AsNoTracking()
            .Where(item => supportFaqIds.Contains(item.SupportFaqId))
            .Select(item => item.SupportFaqId)
            .ToListAsync(cancellationToken);

        var reviewedSupportFaqIds = reviews.ToHashSet();

        var allResolvedSnapshots = resolvedFaqs
            .Select(item =>
            {
                latestSessionsBySupportFaqId.TryGetValue(item.SupportFaqId, out var session);
                var customerName = ResolveCustomerName(session, consumers);
                var agentName = item.AgentUserId.HasValue && agentNames.TryGetValue(item.AgentUserId.Value, out var resolvedAgentName)
                    ? resolvedAgentName
                    : "Unassigned";
                var resolvedAtLabel = item.ResolvedAtUtc.ToString("MMM dd, yyyy hh:mm tt");

                return new QaResolvedTicketSnapshot(
                    item.SupportFaqId,
                    item.AgentUserId,
                    agentName,
                    customerName,
                    item.ResolvedAtUtc,
                    resolvedAtLabel,
                    BuildSearchText(item.SupportFaqId, agentName, customerName, resolvedAtLabel),
                    reviewedSupportFaqIds.Contains(item.SupportFaqId));
            })
            .ToList();

        var normalizedSearch = (search ?? string.Empty).Trim().ToLowerInvariant();
        var filteredResolvedSnapshots = string.IsNullOrWhiteSpace(normalizedSearch)
            ? allResolvedSnapshots
            : allResolvedSnapshots
                .Where(item => item.SearchText.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
                .ToList();

        var pendingItems = filteredResolvedSnapshots
            .Where(item => !item.IsRated)
            .OrderByDescending(item => item.ResolvedAtUtc)
            .Select(item => new QaResolvedTicketItem(
                item.SupportFaqId,
                item.AgentName,
                item.CustomerName,
                item.ResolvedAtLabel))
            .ToList();

        var resolvedCount = filteredResolvedSnapshots.Count;
        var ratedCount = filteredResolvedSnapshots.Count(item => item.IsRated);

        return new QaResolvedTicketsResponse(
            ratedCount,
            resolvedCount,
            pendingItems.Count,
            pendingItems);
    }

    private async Task<Dictionary<int, string>> LoadAgentNamesAsync(int[] agentUserIds, CancellationToken cancellationToken)
    {
        if (agentUserIds.Length == 0)
        {
            return new Dictionary<int, string>();
        }

        var records = new List<QaAgentRecord>();
        var connection = _dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT UserID, AgentName, AgentStatus
                FROM dbo.Agents
                WHERE UserID IS NOT NULL
                """;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var userIdOrdinal = reader.GetOrdinal("UserID");
            var agentNameOrdinal = reader.GetOrdinal("AgentName");
            var agentStatusOrdinal = reader.GetOrdinal("AgentStatus");

            while (await reader.ReadAsync(cancellationToken))
            {
                var userId = reader.GetInt32(userIdOrdinal);
                if (!agentUserIds.Contains(userId))
                {
                    continue;
                }

                records.Add(new QaAgentRecord(
                    userId,
                    reader.IsDBNull(agentNameOrdinal) ? null : reader.GetString(agentNameOrdinal),
                    reader.IsDBNull(agentStatusOrdinal) ? null : reader.GetString(agentStatusOrdinal)));
            }
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }

        return records
            .GroupBy(item => item.UserId)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var availableName = group
                        .Where(item => string.Equals(item.AgentStatus, "available", StringComparison.OrdinalIgnoreCase))
                        .Select(item => item.AgentName)
                        .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name));

                    if (!string.IsNullOrWhiteSpace(availableName))
                    {
                        return availableName.Trim();
                    }

                    var anyName = group
                        .Select(item => item.AgentName)
                        .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name));

                    return string.IsNullOrWhiteSpace(anyName) ? $"Agent {group.Key}" : anyName.Trim();
                });
    }

    private static string ResolveCustomerName(
        LiveAgentSession? session,
        IReadOnlyDictionary<int, ConsumerRef> consumers)
    {
        if (session?.ConsumerId is not int consumerId || !consumers.TryGetValue(consumerId, out var consumer))
        {
            return "Unknown Customer";
        }

        var parts = new[] { consumer.FirstName, consumer.MiddleName, consumer.LastName }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Select(part => part!.Trim())
            .ToArray();

        if (parts.Length > 0)
        {
            return string.Join(' ', parts);
        }

        return string.IsNullOrWhiteSpace(consumer.Username) ? "Unknown Customer" : consumer.Username.Trim();
    }

    private static string BuildSearchText(int supportFaqId, string agentName, string customerName, string resolvedAtLabel)
    {
        return string.Join(' ', supportFaqId, agentName, customerName, resolvedAtLabel).ToLowerInvariant();
    }

    private static (DateTime? StartUtc, DateTime? EndExclusiveUtc) ResolveDateRange(
        string? range,
        DateOnly? from,
        DateOnly? to)
    {
        var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);
        var normalizedRange = (range ?? "custom").Trim().ToLowerInvariant();

        if (normalizedRange == "today")
        {
            var start = todayUtc.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            return (start, start.AddDays(1));
        }

        if (normalizedRange == "last7")
        {
            var startDay = todayUtc.AddDays(-6);
            var start = startDay.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            return (start, todayUtc.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(1));
        }

        if (normalizedRange == "last30")
        {
            var startDay = todayUtc.AddDays(-29);
            var start = startDay.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            return (start, todayUtc.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(1));
        }

        if (from.HasValue && to.HasValue && from > to)
        {
            (from, to) = (to, from);
        }

        var startUtc = from?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endExclusiveUtc = to?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(1);
        return (startUtc, endExclusiveUtc);
    }

    private sealed record ResolvedConversationSeed(
        int SupportFaqId,
        int? AgentUserId,
        DateTime ResolvedAtUtc);

    private sealed record QaResolvedTicketSnapshot(
        int SupportFaqId,
        int? AgentUserId,
        string AgentName,
        string CustomerName,
        DateTime ResolvedAtUtc,
        string ResolvedAtLabel,
        string SearchText,
        bool IsRated);

    private sealed record QaAgentRecord(
        int UserId,
        string? AgentName,
        string? AgentStatus);
}
