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

    public async Task<QaDashboardResponse> GetDashboardAsync(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
    {
        if (fromDate > toDate)
        {
            throw new ArgumentOutOfRangeException(nameof(fromDate), "The from date must be earlier than or equal to the to date.");
        }

        var rangeStartUtc = fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var rangeEndExclusiveUtc = toDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).AddDays(1);
        var monthStartUtc = new DateTime(toDate.Year, toDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEndExclusiveUtc = monthStartUtc.AddMonths(1);
        var selectedMonthLabel = toDate.ToString("MMM yyyy");
        var selectedRangeLabel = BuildRangeLabel(fromDate, toDate);

        var fullMonthResolvedFaqs = await _dbContext.SupportFaqRecords
            .AsNoTracking()
            .Where(item => item.Status == "Resolved"
                && item.EndTime != null
                && item.EndTime >= monthStartUtc
                && item.EndTime < monthEndExclusiveUtc)
            .Select(item => new ResolvedConversationSeed(
                item.Id,
                item.AgentId,
                item.UserType,
                item.Question,
                item.EndTime!.Value))
            .ToListAsync(cancellationToken);

        var monthToDateResolvedFaqs = fullMonthResolvedFaqs
            .Where(item => item.ResolvedAtUtc < rangeEndExclusiveUtc)
            .ToList();

        var rangeResolvedFaqs = await _dbContext.SupportFaqRecords
            .AsNoTracking()
            .Where(item => item.Status == "Resolved"
                && item.EndTime != null
                && item.EndTime >= rangeStartUtc
                && item.EndTime < rangeEndExclusiveUtc)
            .OrderBy(item => item.EndTime)
            .Select(item => new ResolvedConversationSeed(
                item.Id,
                item.AgentId,
                item.UserType,
                item.Question,
                item.EndTime!.Value))
            .ToListAsync(cancellationToken);

        var fullMonthResolvedSupportFaqIds = fullMonthResolvedFaqs
            .Select(item => item.SupportFaqId)
            .Distinct()
            .ToArray();

        var rangeResolvedSupportFaqIds = rangeResolvedFaqs
            .Select(item => item.SupportFaqId)
            .Distinct()
            .ToArray();

        var reviewsForMonthlyResolvedTickets = fullMonthResolvedSupportFaqIds.Length == 0
            ? new List<QaReview>()
            : await _dbContext.QaReviews
                .AsNoTracking()
                .Where(item => fullMonthResolvedSupportFaqIds.Contains(item.SupportFaqId))
                .ToListAsync(cancellationToken);

        var reviewsByMonthlyResolvedSupportFaqId = reviewsForMonthlyResolvedTickets
            .GroupBy(item => item.SupportFaqId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(item => item.UpdatedAtUtc).First());

        var reviewsForResolvedTickets = rangeResolvedSupportFaqIds.Length == 0
            ? new List<QaReview>()
            : await _dbContext.QaReviews
                .AsNoTracking()
                .Where(item => rangeResolvedSupportFaqIds.Contains(item.SupportFaqId)
                    && item.CreatedAtUtc < rangeEndExclusiveUtc)
                .ToListAsync(cancellationToken);

        var reviewsByResolvedSupportFaqId = reviewsForResolvedTickets
            .GroupBy(item => item.SupportFaqId)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(item => item.UpdatedAtUtc).First());

        var fullMonthCreatedReviews = await _dbContext.QaReviews
            .AsNoTracking()
            .Where(item => item.CreatedAtUtc >= monthStartUtc
                && item.CreatedAtUtc < monthEndExclusiveUtc)
            .ToListAsync(cancellationToken);

        var monthToDateCreatedReviews = fullMonthCreatedReviews
            .Where(item => item.CreatedAtUtc < rangeEndExclusiveUtc)
            .ToList();

        var rangeCreatedReviews = await _dbContext.QaReviews
            .AsNoTracking()
            .Where(item => item.CreatedAtUtc >= rangeStartUtc
                && item.CreatedAtUtc < rangeEndExclusiveUtc)
            .ToListAsync(cancellationToken);

        var rangeUpdatedReviews = await _dbContext.QaReviews
            .AsNoTracking()
            .Where(item => item.UpdatedAtUtc >= rangeStartUtc
                && item.UpdatedAtUtc < rangeEndExclusiveUtc
                && item.UpdatedAtUtc > item.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var agentUserIds = fullMonthResolvedFaqs
            .Where(item => item.AgentUserId.HasValue)
            .Select(item => item.AgentUserId!.Value)
            .Concat(fullMonthCreatedReviews.Select(item => item.AgentUserId))
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

        var rangeConversationSnapshots = rangeResolvedFaqs
            .Select(item =>
            {
                var agentName = item.AgentUserId.HasValue && agentNames.TryGetValue(item.AgentUserId.Value, out var resolvedAgentName)
                    ? resolvedAgentName
                    : "Unassigned";
                var concernFrom = QaConcernFormatting.NormalizeConcernFrom(item.UserType);

                return new QaConversationSnapshot(
                    item.SupportFaqId,
                    item.ResolvedAtUtc,
                    item.AgentUserId,
                    agentName,
                    concernFrom,
                    item.Question);
            })
            .ToList();

        var fullMonthConversationSnapshots = fullMonthResolvedFaqs
            .Select(item =>
            {
                var agentName = item.AgentUserId.HasValue && agentNames.TryGetValue(item.AgentUserId.Value, out var resolvedAgentName)
                    ? resolvedAgentName
                    : "Unassigned";
                var concernFrom = QaConcernFormatting.NormalizeConcernFrom(item.UserType);

                return new QaConversationSnapshot(
                    item.SupportFaqId,
                    item.ResolvedAtUtc,
                    item.AgentUserId,
                    agentName,
                    concernFrom,
                    item.Question);
            })
            .ToList();

        var rangeResolvedCount = rangeConversationSnapshots.Count;
        var rangeRatedCount = rangeConversationSnapshots.Count(item => reviewsByResolvedSupportFaqId.ContainsKey(item.SupportFaqId));
        var rangePendingCount = Math.Max(0, rangeResolvedCount - rangeRatedCount);

        var coveragePercent = rangeResolvedCount == 0
            ? 0
            : Math.Round((double)rangeRatedCount / rangeResolvedCount * 100d, 1);

        var averageOverallPercent = rangeCreatedReviews.Count == 0
            ? 0
            : Math.Round((double)rangeCreatedReviews.Average(item => item.OverallPercent), 1);

        var averageQaScore = rangeCreatedReviews.Count == 0
            ? 0
            : Math.Round(averageOverallPercent / 20d, 1);

        var agentMetricPercent = rangeCreatedReviews.Count == 0
            ? 0
            : Math.Round((double)rangeCreatedReviews.Average(item => item.OverallPercent), 1);

        var ratingsUpdated = rangeUpdatedReviews.Count;

        var topAgents = fullMonthCreatedReviews
            .GroupBy(item => item.AgentUserId)
            .Select(group =>
            {
                var agentName = agentNames.TryGetValue(group.Key, out var resolvedAgentName)
                    ? resolvedAgentName
                    : $"Agent {group.Key}";
                var averageQaScore = Math.Round(group.Average(review => (double)review.OverallPercent) / 20d, 1);

                return new
                {
                    group.Key,
                    AgentName = agentName,
                    ReviewCount = group.Count(),
                    AverageQaScore = averageQaScore
                };
            })
            .OrderByDescending(item => item.AverageQaScore)
            .ThenByDescending(item => item.ReviewCount)
            .ThenBy(item => item.AgentName)
            .Take(5)
            .Select(item => new QaDashboardAgentItem(
                item.AgentName,
                $"Completed {item.ReviewCount} QA review{(item.ReviewCount == 1 ? string.Empty : "s")} in {selectedMonthLabel}",
                $"{item.AverageQaScore:0.0} / 5"))
            .ToList();

        var awaitingTickets = fullMonthConversationSnapshots
            .Where(item => !reviewsByMonthlyResolvedSupportFaqId.ContainsKey(item.SupportFaqId))
            .OrderByDescending(item => item.ResolvedAtUtc)
            .Take(5)
            .Select(item => new QaDashboardAwaitingTicketItem(
                item.SupportFaqId,
                item.AgentName,
                item.ConcernFrom,
                item.ResolvedAtUtc.ToString("MMM dd, yyyy hh:mm tt")))
            .ToList();

        var resolvedTrend = new List<int>();
        var ratedTrend = new List<int>();
        for (var day = 1; day <= toDate.Day; day++)
        {
            var bucketDate = new DateOnly(toDate.Year, toDate.Month, day);
            var bucketStartUtc = bucketDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var bucketEndUtc = bucketStartUtc.AddDays(1);

            resolvedTrend.Add(monthToDateResolvedFaqs.Count(item => item.ResolvedAtUtc >= bucketStartUtc && item.ResolvedAtUtc < bucketEndUtc));
            ratedTrend.Add(monthToDateCreatedReviews.Count(item => item.CreatedAtUtc >= bucketStartUtc && item.CreatedAtUtc < bucketEndUtc));
        }

        var throughputPercent = Math.Clamp((int)Math.Round(coveragePercent), 0, 100);
        var ticketsRatedCount = rangeCreatedReviews.Count;

        return new QaDashboardResponse(
            selectedRangeLabel,
            selectedMonthLabel,
            $"{agentMetricPercent:0.0}%",
            rangeResolvedCount,
            ticketsRatedCount.ToString(),
            BuildTicketsRatedSub(ticketsRatedCount, selectedRangeLabel),
            rangePendingCount,
            $"{averageQaScore:0.0} / 5",
            ratingsUpdated,
            BuildThroughputNote(rangeResolvedCount, rangePendingCount, coveragePercent, selectedRangeLabel),
            throughputPercent,
            BuildQualityNote(rangeCreatedReviews.Count, averageQaScore, selectedRangeLabel),
            resolvedTrend,
            ratedTrend,
            topAgents,
            awaitingTickets);
    }

    private static string BuildTicketsRatedSub(int reviewCount, string selectedRangeLabel)
    {
        if (reviewCount == 0)
        {
            return $"No QA reviews were created during {selectedRangeLabel}.";
        }

        return $"{reviewCount} QA review{(reviewCount == 1 ? string.Empty : "s")} created during {selectedRangeLabel}.";
    }

    private static string BuildThroughputNote(int resolvedCount, int pendingCount, double coveragePercent, string selectedRangeLabel)
    {
        if (resolvedCount == 0)
        {
            return $"No resolved live-agent conversations were recorded during {selectedRangeLabel}.";
        }

        if (coveragePercent < 20)
        {
            return $"Only {coveragePercent:0.0}% of tickets resolved during {selectedRangeLabel} are rated. Prioritize the {pendingCount} pending conversation{(pendingCount == 1 ? string.Empty : "s")}.";
        }

        if (coveragePercent < 50)
        {
            return $"QA coverage for {selectedRangeLabel} is {coveragePercent:0.0}%, with {pendingCount} resolved conversation{(pendingCount == 1 ? string.Empty : "s")} still needing review.";
        }

        return $"QA coverage for {selectedRangeLabel} is healthy at {coveragePercent:0.0}%. Keep review work current across the selected range.";
    }

    private static string BuildQualityNote(int reviewCount, double averageQaScore, string selectedRangeLabel)
    {
        if (reviewCount == 0)
        {
            return $"No QA reviews were created during {selectedRangeLabel}.";
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

    private static string BuildRangeLabel(DateOnly fromDate, DateOnly toDate)
    {
        if (fromDate == toDate)
        {
            return fromDate.ToString("MMM dd, yyyy");
        }

        return $"{fromDate:MMM dd, yyyy} to {toDate:MMM dd, yyyy}";
    }

    private sealed record ResolvedConversationSeed(
        int SupportFaqId,
        int? AgentUserId,
        string UserType,
        string Question,
        DateTime ResolvedAtUtc);

    private sealed record QaConversationSnapshot(
        int SupportFaqId,
        DateTime ResolvedAtUtc,
        int? AgentUserId,
        string AgentName,
        string ConcernFrom,
        string Question);
}
