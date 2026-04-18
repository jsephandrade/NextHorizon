using System.Data;
using Microsoft.EntityFrameworkCore;
using NextHorizon.Data;
using NextHorizon.Models.HelpCenter;
using NextHorizon.Models.QA;

namespace NextHorizon.Services;

public sealed class QaAgentTicketsService : IQaAgentTicketsService
{
    private readonly ApplicationDbContext _dbContext;

    public QaAgentTicketsService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<QaAgentTicketsResponse?> GetAgentTicketsAsync(
        int agentUserId,
        string? range,
        DateOnly? from,
        DateOnly? to,
        string? search,
        string? score,
        CancellationToken cancellationToken)
    {
        var (startUtc, endExclusiveUtc) = ResolveDateRange(range, from, to);

        var resolvedFaqQuery = _dbContext.SupportFaqRecords
            .AsNoTracking()
            .Where(item => item.Status == "Resolved"
                && item.EndTime != null);

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

        var agentName = await LoadAgentNameAsync(agentUserId, cancellationToken);

        if (resolvedFaqs.Count == 0)
        {
            return agentName is null
                ? null
                : new QaAgentTicketsResponse(
                    agentUserId,
                    agentName,
                    0,
                    0,
                    Array.Empty<QaAgentTicketItem>(),
                    Array.Empty<QaAgentTicketItem>());
        }

        var supportFaqIds = resolvedFaqs
            .Select(item => item.SupportFaqId)
            .Distinct()
            .ToArray();

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

        var reviews = await _dbContext.QaReviews
            .AsNoTracking()
            .Where(item => supportFaqIds.Contains(item.SupportFaqId) && item.AgentUserId == agentUserId)
            .ToListAsync(cancellationToken);

        var reviewBySupportFaqId = reviews
            .GroupBy(item => item.SupportFaqId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(item => item.UpdatedAtUtc).First());

        var normalizedSearch = (search ?? string.Empty).Trim().ToLowerInvariant();

        var snapshots = resolvedFaqs
            .Select(item =>
            {
                latestSessionsBySupportFaqId.TryGetValue(item.SupportFaqId, out var session);
                var customerName = ResolveCustomerName(session, consumers);
                reviewBySupportFaqId.TryGetValue(item.SupportFaqId, out var review);
                var resolvedAtLabel = item.ResolvedAtUtc.ToString("MMM dd, yyyy hh:mm tt");

                return new QaAgentTicketSnapshot(
                    item.SupportFaqId,
                    agentName ?? $"Agent {agentUserId}",
                    customerName,
                    item.ResolvedAtUtc,
                    resolvedAtLabel,
                    item.AgentUserId == agentUserId,
                    review);
            })
            .Where(item => item.IsCurrentAgentAssignment || item.Review is not null)
            .Where(item => string.IsNullOrWhiteSpace(normalizedSearch)
                || BuildSearchText(item.SupportFaqId, item.AgentName, item.CustomerName, item.ResolvedAtLabel)
                    .Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(item => item.ResolvedAtUtc)
            .ThenByDescending(item => item.SupportFaqId)
            .ToList();

        var awaitingItems = snapshots
            .Where(item => item.Review is null)
            .Select(item => new QaAgentTicketItem(
                item.SupportFaqId,
                item.AgentName,
                item.CustomerName,
                item.ResolvedAtLabel,
                item.ResolvedAtUtc.ToString("yyyy-MM-dd"),
                false,
                null))
            .ToList();

        var ratedItems = snapshots
            .Where(item => item.Review is not null)
            .Where(item => MatchesScoreBand((double)item.Review!.OverallPercent, score))
            .Select(item => new QaAgentTicketItem(
                item.SupportFaqId,
                item.AgentName,
                item.CustomerName,
                item.ResolvedAtLabel,
                item.ResolvedAtUtc.ToString("yyyy-MM-dd"),
                true,
                Math.Round((double)item.Review!.OverallPercent, 1)))
            .ToList();

        return new QaAgentTicketsResponse(
            agentUserId,
            agentName ?? $"Agent {agentUserId}",
            awaitingItems.Count,
            ratedItems.Count,
            awaitingItems,
            ratedItems);
    }

    private async Task<string?> LoadAgentNameAsync(int agentUserId, CancellationToken cancellationToken)
    {
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
                WHERE UserID = @UserID
                  AND UserID IS NOT NULL
                """;

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@UserID";
            parameter.Value = agentUserId;
            command.Parameters.Add(parameter);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var rows = new List<QaAgentRecord>();
            var userIdOrdinal = reader.GetOrdinal("UserID");
            var agentNameOrdinal = reader.GetOrdinal("AgentName");
            var agentStatusOrdinal = reader.GetOrdinal("AgentStatus");

            while (await reader.ReadAsync(cancellationToken))
            {
                rows.Add(new QaAgentRecord(
                    reader.GetInt32(userIdOrdinal),
                    reader.IsDBNull(agentNameOrdinal) ? null : reader.GetString(agentNameOrdinal),
                    reader.IsDBNull(agentStatusOrdinal) ? null : reader.GetString(agentStatusOrdinal)));
            }

            var availableName = rows
                .Where(item => string.Equals(item.AgentStatus, "available", StringComparison.OrdinalIgnoreCase))
                .Select(item => item.AgentName)
                .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name));

            if (!string.IsNullOrWhiteSpace(availableName))
            {
                return availableName.Trim();
            }

            var anyName = rows
                .Select(item => item.AgentName)
                .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name));

            return string.IsNullOrWhiteSpace(anyName) ? null : anyName.Trim();
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
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

    private sealed record ResolvedConversationSeed(
        int SupportFaqId,
        int? AgentUserId,
        DateTime ResolvedAtUtc);

    private sealed record QaAgentTicketSnapshot(
        int SupportFaqId,
        string AgentName,
        string CustomerName,
        DateTime ResolvedAtUtc,
        string ResolvedAtLabel,
        bool IsCurrentAgentAssignment,
        QaReview? Review);

    private sealed record QaAgentRecord(
        int UserId,
        string? AgentName,
        string? AgentStatus);
}
