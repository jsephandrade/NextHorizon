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

        var monthResolvedFaqs = await _dbContext.SupportFaqRecords
            .AsNoTracking()
            .Where(item => item.Status == "Resolved"
                && item.EndTime != null
                && item.EndTime >= monthStartUtc
                && item.EndTime < dayEndExclusiveUtc)
            .Select(item => new ResolvedConversationSeed(
                item.Id,
                item.AgentId,
                item.Question,
                item.EndTime!.Value))
            .ToListAsync(cancellationToken);

        var dayResolvedFaqs = monthResolvedFaqs
            .Where(item => item.ResolvedAtUtc >= dayStartUtc && item.ResolvedAtUtc < dayEndExclusiveUtc)
            .OrderBy(item => item.ResolvedAtUtc)
            .ToList();

        var dayResolvedSupportFaqIds = dayResolvedFaqs
            .Select(item => item.SupportFaqId)
            .Distinct()
            .ToArray();

        var reviewsForResolvedTickets = dayResolvedSupportFaqIds.Length == 0
            ? new List<QaReview>()
            : await _dbContext.QaReviews
                .AsNoTracking()
                .Where(item => dayResolvedSupportFaqIds.Contains(item.SupportFaqId)
                    && item.CreatedAtUtc < dayEndExclusiveUtc)
                .ToListAsync(cancellationToken);

        var reviewsByResolvedSupportFaqId = reviewsForResolvedTickets
            .GroupBy(item => item.SupportFaqId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(item => item.UpdatedAtUtc).First());

        var monthCreatedReviews = await _dbContext.QaReviews
            .AsNoTracking()
            .Where(item => item.CreatedAtUtc >= monthStartUtc
                && item.CreatedAtUtc < dayEndExclusiveUtc)
            .ToListAsync(cancellationToken);

        var dayCreatedReviews = monthCreatedReviews
            .Where(item => item.CreatedAtUtc >= dayStartUtc && item.CreatedAtUtc < dayEndExclusiveUtc)
            .ToList();

        var dayUpdatedReviews = await _dbContext.QaReviews
            .AsNoTracking()
            .Where(item => item.UpdatedAtUtc >= dayStartUtc
                && item.UpdatedAtUtc < dayEndExclusiveUtc
                && item.UpdatedAtUtc > item.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var sessions = dayResolvedSupportFaqIds.Length == 0
            ? new List<LiveAgentSession>()
            : await _dbContext.LiveAgentSessions
                .AsNoTracking()
                .Where(item => dayResolvedSupportFaqIds.Contains(item.SupportFaqId))
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

        var agentUserIds = dayResolvedFaqs
            .Where(item => item.AgentUserId.HasValue)
            .Select(item => item.AgentUserId!.Value)
            .Concat(dayCreatedReviews.Select(item => item.AgentUserId))
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

        var dayConversationSnapshots = dayResolvedFaqs
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

        var dayResolvedCount = dayConversationSnapshots.Count;
        var dayRatedCount = dayConversationSnapshots.Count(item => reviewsByResolvedSupportFaqId.ContainsKey(item.SupportFaqId));
        var dayPendingCount = Math.Max(0, dayResolvedCount - dayRatedCount);

        var coveragePercent = dayResolvedCount == 0
            ? 0
            : Math.Round((double)dayRatedCount / dayResolvedCount * 100d, 1);

        var averageOverallPercent = dayCreatedReviews.Count == 0
            ? 0
            : Math.Round((double)dayCreatedReviews.Average(item => item.OverallPercent), 1);

        var averageQaScore = dayCreatedReviews.Count == 0
            ? 0
            : Math.Round(averageOverallPercent / 20d, 1);

        var agentMetricPercent = dayCreatedReviews.Count == 0
            ? 0
            : Math.Round((double)dayCreatedReviews.Average(item => item.OverallPercent), 1);

        var ratingsUpdated = dayUpdatedReviews.Count;

        var topAgents = dayCreatedReviews
            .GroupBy(item => item.AgentUserId)
            .Select(group =>
            {
                var agentName = agentNames.TryGetValue(group.Key, out var resolvedAgentName)
                    ? resolvedAgentName
                    : $"Agent {group.Key}";

                return new
                {
                    group.Key,
                    AgentName = agentName,
                    Reviews = group.ToList()
                };
            })
            .OrderByDescending(item => item.Reviews.Count)
            .ThenBy(item => item.AgentName)
            .Take(5)
            .Select(item =>
            {
                var qaScoreLabel = item.Reviews.Count == 0
                    ? "No QA score"
                    : $"{Math.Round(item.Reviews.Average(review => (double)review.OverallPercent) / 20d, 1):0.0} / 5";

                return new QaDashboardAgentItem(
                    item.AgentName,
                    $"Completed {item.Reviews.Count} QA review{(item.Reviews.Count == 1 ? string.Empty : "s")} on {selectedDate:MMM dd, yyyy}",
                    qaScoreLabel);
            })
            .ToList();

        var awaitingTickets = dayConversationSnapshots
            .Where(item => !reviewsByResolvedSupportFaqId.ContainsKey(item.SupportFaqId))
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

            resolvedTrend.Add(monthResolvedFaqs.Count(item => item.ResolvedAtUtc >= bucketStartUtc && item.ResolvedAtUtc < bucketEndUtc));
            ratedTrend.Add(monthCreatedReviews.Count(item => item.CreatedAtUtc >= bucketStartUtc && item.CreatedAtUtc < bucketEndUtc));
        }

        var throughputPercent = Math.Clamp((int)Math.Round(coveragePercent), 0, 100);
        var ticketsRatedCount = dayCreatedReviews.Count;

        return new QaDashboardResponse(
            selectedDate.ToString("MMM dd, yyyy"),
            $"{agentMetricPercent:0.0}%",
            dayPendingCount,
            ticketsRatedCount.ToString(),
            BuildTicketsRatedSub(ticketsRatedCount, selectedDate),
            dayPendingCount,
            $"{averageQaScore:0.0} / 5",
            ratingsUpdated,
            BuildThroughputNote(dayResolvedCount, dayPendingCount, coveragePercent, selectedDate),
            throughputPercent,
            BuildQualityNote(dayCreatedReviews.Count, averageQaScore, selectedDate),
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

    private static string BuildTicketsRatedSub(int reviewCount, DateOnly selectedDate)
    {
        if (reviewCount == 0)
        {
            return $"No QA reviews were created on {selectedDate:MMM dd, yyyy}.";
        }

        return $"{reviewCount} QA review{(reviewCount == 1 ? string.Empty : "s")} created on {selectedDate:MMM dd, yyyy}.";
    }

    private static string BuildThroughputNote(int resolvedCount, int pendingCount, double coveragePercent, DateOnly selectedDate)
    {
        if (resolvedCount == 0)
        {
            return $"No resolved live-agent conversations were recorded on {selectedDate:MMM dd, yyyy}.";
        }

        if (coveragePercent < 20)
        {
            return $"Only {coveragePercent:0.0}% of tickets resolved on {selectedDate:MMM dd, yyyy} are rated. Prioritize the {pendingCount} pending conversation{(pendingCount == 1 ? string.Empty : "s")}.";
        }

        if (coveragePercent < 50)
        {
            return $"QA coverage for {selectedDate:MMM dd, yyyy} is {coveragePercent:0.0}%, with {pendingCount} resolved conversation{(pendingCount == 1 ? string.Empty : "s")} still needing review.";
        }

        return $"QA coverage for {selectedDate:MMM dd, yyyy} is healthy at {coveragePercent:0.0}%. Keep same-day reviews current on newly resolved conversations.";
    }

    private static string BuildQualityNote(int reviewCount, double averageQaScore, DateOnly selectedDate)
    {
        if (reviewCount == 0)
        {
            return $"No QA reviews were created on {selectedDate:MMM dd, yyyy}.";
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
