using Microsoft.EntityFrameworkCore;
using NextHorizon.Data;
using NextHorizon.Models.HelpCenter;
using NextHorizon.Models.QA;

namespace NextHorizon.Services;

public sealed class QaDashboardService : IQaDashboardService
{
    private readonly ApplicationDbContext _dbContext;

    public QaDashboardService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<QaDashboardResponse> GetDashboardAsync(DateOnly selectedDate, CancellationToken cancellationToken)
    {
        var dayStartUtc = selectedDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dayEndExclusiveUtc = dayStartUtc.AddDays(1);
        var monthStartUtc = new DateTime(selectedDate.Year, selectedDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var trailingWeekStartUtc = dayStartUtc.AddDays(-6);

        var resolvedFaqs = await _dbContext.SupportFaqRecords
            .AsNoTracking()
            .Where(item => item.Status == "Resolved"
                && item.EndTime != null
                && item.EndTime < dayEndExclusiveUtc)
            .Select(item => new ResolvedConversationSeed(
                item.Id,
                item.AgentId,
                item.Question,
                item.EndTime!.Value))
            .ToListAsync(cancellationToken);

        if (resolvedFaqs.Count == 0)
        {
            return new QaDashboardResponse(
                selectedDate.ToString("MMM dd, yyyy"),
                "0.0%",
                0,
                "0 / 0",
                "0.0% QA completion coverage",
                0,
                "0.0 / 5",
                0,
                "No resolved live-agent conversations for the selected period.",
                0,
                "No QA reviews are available yet.",
                Array.Empty<int>(),
                Array.Empty<int>(),
                Array.Empty<QaDashboardAgentItem>(),
                Array.Empty<QaDashboardAwaitingTicketItem>());
        }

        var supportFaqIds = resolvedFaqs.Select(item => item.SupportFaqId).Distinct().ToArray();
        var sessions = await _dbContext.LiveAgentSessions
            .AsNoTracking()
            .Where(item => supportFaqIds.Contains(item.SupportFaqId))
            .ToListAsync(cancellationToken);

        var latestSessionsBySupportFaqId = sessions
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

        var agentNames = agentUserIds.Length == 0
            ? new Dictionary<int, string>()
            : (await _dbContext.SupportAgents
                .AsNoTracking()
                .Where(item => agentUserIds.Contains(item.UserId))
                .ToListAsync(cancellationToken))
                .GroupBy(item => item.UserId)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .OrderByDescending(item => item.AgentStatus == "available")
                        .ThenByDescending(item => item.ChatId)
                        .Select(item => string.IsNullOrWhiteSpace(item.AgentName) ? "Unknown Agent" : item.AgentName!.Trim())
                        .First());

        var conversationSnapshots = resolvedFaqs
            .Select(item =>
            {
                latestSessionsBySupportFaqId.TryGetValue(item.SupportFaqId, out var session);
                var customerName = ResolveCustomerName(session, consumers);
                var agentName = item.AgentUserId.HasValue && agentNames.TryGetValue(item.AgentUserId.Value, out var resolvedAgentName)
                    ? resolvedAgentName
                    : "Unassigned";

                return new QaConversationSnapshot(
                    item.SupportFaqId,
                    item.ResolvedAtUtc,
                    item.AgentUserId,
                    agentName,
                    customerName,
                    item.Question);
            })
            .ToList();

        var reviews = await _dbContext.QaReviews
            .AsNoTracking()
            .Where(item => supportFaqIds.Contains(item.SupportFaqId)
                && item.CreatedAtUtc < dayEndExclusiveUtc)
            .ToListAsync(cancellationToken);

        var reviewBySupportFaqId = reviews
            .GroupBy(item => item.SupportFaqId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(item => item.UpdatedAtUtc).First());

        var monthSnapshots = conversationSnapshots
            .Where(item => item.ResolvedAtUtc >= monthStartUtc)
            .OrderBy(item => item.ResolvedAtUtc)
            .ToList();

        var trailingWeekSnapshots = conversationSnapshots
            .Where(item => item.ResolvedAtUtc >= trailingWeekStartUtc)
            .OrderBy(item => item.ResolvedAtUtc)
            .ToList();

        var monthResolvedCount = monthSnapshots.Count;
        var monthRatedCount = monthSnapshots.Count(item => reviewBySupportFaqId.ContainsKey(item.SupportFaqId));
        var monthPendingCount = Math.Max(0, monthResolvedCount - monthRatedCount);
        var dayPendingCount = conversationSnapshots.Count(item => !reviewBySupportFaqId.ContainsKey(item.SupportFaqId));

        var monthReviews = reviews
            .Where(item => item.UpdatedAtUtc >= monthStartUtc && item.UpdatedAtUtc < dayEndExclusiveUtc)
            .ToList();

        var coveragePercent = monthResolvedCount == 0
            ? 0
            : Math.Round((double)monthRatedCount / monthResolvedCount * 100d, 1);

        var averageOverallPercent = monthReviews.Count == 0
            ? 0
            : Math.Round((double)monthReviews.Average(item => item.OverallPercent), 1);

        var averageQaScore = monthReviews.Count == 0
            ? 0
            : Math.Round(averageOverallPercent / 20d, 1);

        var agentMetricPercent = monthReviews.Count == 0
            ? 0
            : Math.Round((double)monthReviews.Average(item => item.OverallPercent), 1);

        var ratingsUpdated = monthReviews.Count(item => item.UpdatedAtUtc > item.CreatedAtUtc);

        var topAgents = trailingWeekSnapshots
            .GroupBy(item => new { item.AgentUserId, item.AgentName })
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key.AgentName)
            .Take(5)
            .Select(group =>
            {
                var reviewedItems = group
                    .Where(item => reviewBySupportFaqId.ContainsKey(item.SupportFaqId))
                    .Select(item => reviewBySupportFaqId[item.SupportFaqId])
                    .ToList();

                var qaScoreLabel = reviewedItems.Count == 0
                    ? "No QA score"
                    : $"{Math.Round(reviewedItems.Average(item => (double)item.OverallPercent) / 20d, 1):0.0} / 5";

                return new QaDashboardAgentItem(
                    group.Key.AgentName,
                    $"Resolved {group.Count()} tickets in the last 7 days",
                    qaScoreLabel);
            })
            .ToList();

        var awaitingTickets = conversationSnapshots
            .Where(item => !reviewBySupportFaqId.ContainsKey(item.SupportFaqId))
            .OrderByDescending(item => item.ResolvedAtUtc)
            .Take(5)
            .Select(item => new QaDashboardAwaitingTicketItem(
                item.SupportFaqId,
                item.AgentName,
                item.CustomerName,
                item.ResolvedAtUtc.ToString("MMM dd, yyyy hh:mm tt")))
            .ToList();

        var resolvedTrend = new List<int>();
        var ratedTrend = new List<int>();
        for (var day = 1; day <= selectedDate.Day; day++)
        {
            var bucketDate = new DateOnly(selectedDate.Year, selectedDate.Month, day);
            var bucketStartUtc = bucketDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var bucketEndUtc = bucketStartUtc.AddDays(1);

            var dayItems = monthSnapshots
                .Where(item => item.ResolvedAtUtc >= bucketStartUtc && item.ResolvedAtUtc < bucketEndUtc)
                .ToList();

            resolvedTrend.Add(dayItems.Count);
            ratedTrend.Add(dayItems.Count(item => reviewBySupportFaqId.ContainsKey(item.SupportFaqId)));
        }

        var throughputPercent = Math.Clamp((int)Math.Round(coveragePercent), 0, 100);

        return new QaDashboardResponse(
            selectedDate.ToString("MMM dd, yyyy"),
            $"{agentMetricPercent:0.0}%",
            dayPendingCount,
            $"{monthRatedCount} / {monthResolvedCount}",
            $"{coveragePercent:0.0}% QA completion coverage",
            monthPendingCount,
            $"{averageQaScore:0.0} / 5",
            ratingsUpdated,
            BuildThroughputNote(monthResolvedCount, monthPendingCount, coveragePercent),
            throughputPercent,
            BuildQualityNote(monthReviews.Count, averageQaScore),
            resolvedTrend,
            ratedTrend,
            topAgents,
            awaitingTickets);
    }

    private static string ResolveCustomerName(LiveAgentSession? session, IReadOnlyDictionary<int, ConsumerRef> consumers)
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

    private static string BuildThroughputNote(int monthResolvedCount, int pendingCount, double coveragePercent)
    {
        if (monthResolvedCount == 0)
        {
            return "No resolved live-agent conversations for the selected month.";
        }

        if (coveragePercent < 20)
        {
            return $"Only {coveragePercent:0.0}% of resolved tickets are rated. Prioritize the oldest {pendingCount} pending conversations first.";
        }

        if (coveragePercent < 50)
        {
            return $"QA coverage is improving at {coveragePercent:0.0}%, but {pendingCount} resolved conversations still need review.";
        }

        return $"QA coverage is healthy at {coveragePercent:0.0}% for the selected month. Keep reviews current on newly resolved conversations.";
    }

    private static string BuildQualityNote(int reviewCount, double averageQaScore)
    {
        if (reviewCount == 0)
        {
            return "No QA reviews are available yet for the selected month.";
        }

        if (averageQaScore < 3.5)
        {
            return "Average QA score is below target. Review low-scoring conversations for focused coaching.";
        }

        if (averageQaScore < 4.5)
        {
            return "Average QA score is stable. Monitor conversations below 4.0/5 and use edits for calibration.";
        }

        return "Average QA score is strong. Keep monitoring low outliers and edited reviews for consistency.";
    }

    private sealed record ResolvedConversationSeed(
        int SupportFaqId,
        int? AgentUserId,
        string Question,
        DateTime ResolvedAtUtc);

    private sealed record QaConversationSnapshot(
        int SupportFaqId,
        DateTime ResolvedAtUtc,
        int? AgentUserId,
        string AgentName,
        string CustomerName,
        string Question);
}
