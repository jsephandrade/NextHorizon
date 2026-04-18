using System.Data;
using Microsoft.EntityFrameworkCore;
using NextHorizon.Data;
using NextHorizon.Models.HelpCenter;
using NextHorizon.Models.QA;

namespace NextHorizon.Services;

public sealed class QaRatingQueueService : IQaRatingQueueService
{
    private readonly ApplicationDbContext _dbContext;

    public QaRatingQueueService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<QaRatingQueueResponse> GetQueueAsync(
        string? range,
        DateOnly? from,
        DateOnly? to,
        string? search,
        int? ticketId,
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
                item.UserType,
                item.EndTime!.Value))
            .ToListAsync(cancellationToken);

        if (resolvedFaqs.Count == 0)
        {
            return new QaRatingQueueResponse(0, null, false, null, null, null, Array.Empty<QaRatingQueueItem>(), null, null);
        }

        var supportFaqIds = resolvedFaqs
            .Select(item => item.SupportFaqId)
            .Distinct()
            .ToArray();

        var agentUserIds = resolvedFaqs
            .Where(item => item.AgentUserId.HasValue)
            .Select(item => item.AgentUserId!.Value)
            .Distinct()
            .ToArray();

        var agentNames = await LoadAgentNamesAsync(agentUserIds, cancellationToken);

        var reviewedSupportFaqIds = await _dbContext.QaReviews
            .AsNoTracking()
            .Where(item => supportFaqIds.Contains(item.SupportFaqId))
            .Select(item => item.SupportFaqId)
            .ToHashSetAsync(cancellationToken);

        var normalizedSearch = (search ?? string.Empty).Trim();

        var resolvedSnapshots = resolvedFaqs
            .Select(item =>
            {
                var agentName = item.AgentUserId.HasValue && agentNames.TryGetValue(item.AgentUserId.Value, out var resolvedAgentName)
                    ? resolvedAgentName
                    : "Unassigned";
                var concernFrom = QaConcernFormatting.NormalizeConcernFrom(item.UserType);
                var resolvedAtLabel = item.ResolvedAtUtc.ToString("MMM dd, yyyy hh:mm tt");

                return new QaRatingQueueSnapshot(
                    item.SupportFaqId,
                    item.AgentUserId,
                    agentName,
                    concernFrom,
                    item.ResolvedAtUtc,
                    resolvedAtLabel,
                    BuildSearchText(item.SupportFaqId, agentName, concernFrom, resolvedAtLabel),
                    reviewedSupportFaqIds.Contains(item.SupportFaqId));
            })
            .OrderByDescending(item => item.ResolvedAtUtc)
            .ThenByDescending(item => item.SupportFaqId)
            .ToList();

        var filteredResolvedSnapshots = string.IsNullOrWhiteSpace(normalizedSearch)
            ? resolvedSnapshots
            : resolvedSnapshots
                .Where(item => item.SearchText.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
                .ToList();

        var pendingQueue = filteredResolvedSnapshots
            .Where(item => !item.IsRated)
            .ToList();

        var currentIndex = ticketId.HasValue
            ? pendingQueue.FindIndex(item => item.SupportFaqId == ticketId.Value)
            : -1;

        var currentTicketInQueue = currentIndex >= 0;
        int? previousSupportFaqId = currentTicketInQueue && currentIndex > 0
            ? pendingQueue[currentIndex - 1].SupportFaqId
            : null;
        int? nextSupportFaqId = currentTicketInQueue && currentIndex < pendingQueue.Count - 1
            ? pendingQueue[currentIndex + 1].SupportFaqId
            : null;
        var currentFilteredIndex = ticketId.HasValue
            ? filteredResolvedSnapshots.FindIndex(item => item.SupportFaqId == ticketId.Value)
            : -1;
        var hasFilteredContext = currentFilteredIndex >= 0;
        int? previousFilteredSupportFaqId = hasFilteredContext && currentFilteredIndex > 0
            ? filteredResolvedSnapshots[currentFilteredIndex - 1].SupportFaqId
            : null;
        int? nextFilteredSupportFaqId = hasFilteredContext && currentFilteredIndex < filteredResolvedSnapshots.Count - 1
            ? filteredResolvedSnapshots[currentFilteredIndex + 1].SupportFaqId
            : null;

        var matchedSupportFaqId = ResolveMatchedSupportFaqId(filteredResolvedSnapshots, pendingQueue, normalizedSearch);

        return new QaRatingQueueResponse(
            pendingQueue.Count,
            currentTicketInQueue ? currentIndex + 1 : null,
            currentTicketInQueue,
            previousSupportFaqId,
            nextSupportFaqId,
            matchedSupportFaqId,
            pendingQueue.Select(item => new QaRatingQueueItem(
                    item.SupportFaqId,
                    item.AgentName,
                    item.ConcernFrom,
                    item.ResolvedAtLabel,
                    item.IsRated))
                .ToList(),
            previousFilteredSupportFaqId,
            nextFilteredSupportFaqId);
    }

    private static int? ResolveMatchedSupportFaqId(
        IReadOnlyList<QaRatingQueueSnapshot> filteredResolvedSnapshots,
        IReadOnlyList<QaRatingQueueSnapshot> pendingQueue,
        string normalizedSearch)
    {
        if (string.IsNullOrWhiteSpace(normalizedSearch))
        {
            return pendingQueue.FirstOrDefault()?.SupportFaqId;
        }

        if (int.TryParse(normalizedSearch, out var parsedTicketId))
        {
            var exactMatch = filteredResolvedSnapshots.FirstOrDefault(item => item.SupportFaqId == parsedTicketId);
            if (exactMatch is not null)
            {
                return exactMatch.SupportFaqId;
            }
        }

        return filteredResolvedSnapshots.FirstOrDefault()?.SupportFaqId;
    }

    private async Task<Dictionary<int, string>> LoadAgentNamesAsync(int[] agentUserIds, CancellationToken cancellationToken)
    {
        if (agentUserIds.Length == 0)
        {
            return new Dictionary<int, string>();
        }

        var records = new List<QaAgentRecord>();
        var agentUserIdSet = agentUserIds.ToHashSet();
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
                if (!agentUserIdSet.Contains(userId))
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

    private static string BuildSearchText(int supportFaqId, string agentName, string concernFrom, string resolvedAtLabel)
    {
        return string.Join(' ', supportFaqId, agentName, concernFrom, resolvedAtLabel).ToLowerInvariant();
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
        string UserType,
        DateTime ResolvedAtUtc);

    private sealed record QaRatingQueueSnapshot(
        int SupportFaqId,
        int? AgentUserId,
        string AgentName,
        string ConcernFrom,
        DateTime ResolvedAtUtc,
        string ResolvedAtLabel,
        string SearchText,
        bool IsRated);

    private sealed record QaAgentRecord(
        int UserId,
        string? AgentName,
        string? AgentStatus);
}
