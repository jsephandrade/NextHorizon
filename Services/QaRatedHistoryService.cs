using System.Data;
using Microsoft.EntityFrameworkCore;
using NextHorizon.Data;
using NextHorizon.Models.HelpCenter;
using NextHorizon.Models.QA;

namespace NextHorizon.Services;

public sealed class QaRatedHistoryService : IQaRatedHistoryService
{
    private readonly ApplicationDbContext _dbContext;

    public QaRatedHistoryService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<QaRatedHistoryResponse> GetRatedHistoryAsync(
        string? range,
        DateOnly? from,
        DateOnly? to,
        string? search,
        string? score,
        CancellationToken cancellationToken)
    {
        var (startUtc, endExclusiveUtc) = ResolveDateRange(range, from, to);

        var reviewQuery = _dbContext.QaReviews
            .AsNoTracking();

        if (startUtc.HasValue)
        {
            reviewQuery = reviewQuery.Where(item => item.CreatedAtUtc >= startUtc.Value);
        }

        if (endExclusiveUtc.HasValue)
        {
            reviewQuery = reviewQuery.Where(item => item.CreatedAtUtc < endExclusiveUtc.Value);
        }

        var reviews = await reviewQuery
            .OrderByDescending(item => item.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        if (reviews.Count == 0)
        {
            return new QaRatedHistoryResponse(0, Array.Empty<QaRatedHistoryItem>());
        }

        var supportFaqIds = reviews
            .Select(item => item.SupportFaqId)
            .Distinct()
            .ToArray();

        var faqs = await _dbContext.SupportFaqRecords
            .AsNoTracking()
            .Where(item => supportFaqIds.Contains(item.Id))
            .Select(item => new SupportFaqSeed(item.Id, item.AgentId))
            .ToListAsync(cancellationToken);

        var faqById = faqs.ToDictionary(item => item.SupportFaqId);

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

        var agentUserIds = faqs
            .Where(item => item.AgentUserId.HasValue)
            .Select(item => item.AgentUserId!.Value)
            .Distinct()
            .ToArray();

        var agentNames = await LoadAgentNamesAsync(agentUserIds, cancellationToken);
        var normalizedSearch = (search ?? string.Empty).Trim().ToLowerInvariant();

        var items = reviews
            .Where(item => MatchesScoreBand((double)item.OverallPercent, score))
            .Select(item =>
            {
                faqById.TryGetValue(item.SupportFaqId, out var faq);
                latestSessionsBySupportFaqId.TryGetValue(item.SupportFaqId, out var session);

                var agentName = faq?.AgentUserId is int userId && agentNames.TryGetValue(userId, out var resolvedAgentName)
                    ? resolvedAgentName
                    : "Unassigned";
                var customerName = ResolveCustomerName(session, consumers);
                var ratedAtLabel = item.CreatedAtUtc.ToString("MMM dd, yyyy");

                return new QaRatedHistoryItem(
                    item.SupportFaqId,
                    agentName,
                    customerName,
                    ratedAtLabel,
                    item.CreatedAtUtc.ToString("yyyy-MM-dd"),
                    Math.Round((double)((item.AccuracyAverage / 5m) * 35m), 1),
                    Math.Round((double)((item.ToneAverage / 5m) * 35m), 1),
                    Math.Round((double)((item.ResolutionAverage / 5m) * 30m), 1),
                    Math.Round((double)item.OverallPercent, 1));
            })
            .Where(item => string.IsNullOrWhiteSpace(normalizedSearch)
                || BuildSearchText(item).Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return new QaRatedHistoryResponse(items.Count, items);
    }

    private async Task<Dictionary<int, string>> LoadAgentNamesAsync(int[] agentUserIds, CancellationToken cancellationToken)
    {
        if (agentUserIds.Length == 0)
        {
            return new Dictionary<int, string>();
        }

        var rows = new List<QaAgentRecord>();
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

                rows.Add(new QaAgentRecord(
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

        return rows
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

    private static string BuildSearchText(QaRatedHistoryItem item)
    {
        return string.Join(' ', item.SupportFaqId, item.AgentName, item.CustomerName, item.RatedAtLabel).ToLowerInvariant();
    }

    private static bool MatchesScoreBand(double overallPercent, string? score)
    {
        var normalized = (score ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "high" => overallPercent >= 90d && overallPercent <= 100d,
            "good" => overallPercent >= 80d && overallPercent < 90d,
            "mid" => overallPercent >= 70d && overallPercent < 80d,
            "low" => overallPercent < 70d,
            _ => true
        };
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

        return (
            from?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            to?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(1));
    }

    private sealed record SupportFaqSeed(
        int SupportFaqId,
        int? AgentUserId);

    private sealed record QaAgentRecord(
        int UserId,
        string? AgentName,
        string? AgentStatus);
}
