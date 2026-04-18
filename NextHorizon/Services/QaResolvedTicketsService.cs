using System.Data;
using Microsoft.EntityFrameworkCore;
using NextHorizon.Data;
using NextHorizon.Models.HelpCenter;
using NextHorizon.Models.QA;

namespace NextHorizon.Services;

public sealed class QaResolvedTicketsService : IQaResolvedTicketsService
{
    private const int DefaultPageSize = 10;
    private readonly ApplicationDbContext _dbContext;

    public QaResolvedTicketsService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<QaResolvedTicketsResponse> GetResolvedTicketsAsync(
        int page,
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
                item.UserType,
                item.EndTime!.Value))
            .ToListAsync(cancellationToken);

        if (resolvedFaqs.Count == 0)
        {
            return new QaResolvedTicketsResponse(0, 0, 0, 1, DefaultPageSize, 0, 1, false, false, Array.Empty<QaResolvedTicketItem>());
        }

        var supportFaqIds = resolvedFaqs.Select(item => item.SupportFaqId).Distinct().ToArray();

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
                var agentName = item.AgentUserId.HasValue && agentNames.TryGetValue(item.AgentUserId.Value, out var resolvedAgentName)
                    ? resolvedAgentName
                    : "Unassigned";
                var concernFrom = QaConcernFormatting.NormalizeConcernFrom(item.UserType);
                var resolvedAtLabel = item.ResolvedAtUtc.ToString("MMM dd, yyyy hh:mm tt");

                return new QaResolvedTicketSnapshot(
                    item.SupportFaqId,
                    item.AgentUserId,
                    agentName,
                    concernFrom,
                    item.ResolvedAtUtc,
                    resolvedAtLabel,
                    BuildSearchText(item.SupportFaqId, agentName, concernFrom, resolvedAtLabel),
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
                item.ConcernFrom,
                item.ResolvedAtLabel))
            .ToList();

        var resolvedCount = filteredResolvedSnapshots.Count;
        var ratedCount = filteredResolvedSnapshots.Count(item => item.IsRated);
        var totalPendingCount = pendingItems.Count;
        var totalPages = totalPendingCount == 0
            ? 1
            : (int)Math.Ceiling(totalPendingCount / (double)DefaultPageSize);
        var currentPage = Math.Clamp(page, 1, totalPages);
        var pagedItems = pendingItems
            .Skip((currentPage - 1) * DefaultPageSize)
            .Take(DefaultPageSize)
            .ToList();

        return new QaResolvedTicketsResponse(
            ratedCount,
            resolvedCount,
            totalPendingCount,
            currentPage,
            DefaultPageSize,
            totalPendingCount,
            totalPages,
            currentPage > 1,
            currentPage < totalPages,
            pagedItems);
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

    private sealed record QaResolvedTicketSnapshot(
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
