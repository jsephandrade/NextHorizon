using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NextHorizon.Data;
using NextHorizon.Models.Agent;
using NextHorizon.Models.HelpCenter;
using NextHorizon.Models.QA;

namespace NextHorizon.Services;

public sealed class QaReviewService : IQaReviewService
{
    private static readonly string[] AccuracyKeys = ["accuracy_q1", "accuracy_q2", "accuracy_q3"];
    private static readonly string[] ToneKeys = ["tone_q1", "tone_q2", "tone_q3"];
    private static readonly string[] ResolutionKeys = ["resolution_q1", "resolution_q2", "resolution_q3"];

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly ApplicationDbContext _dbContext;
    private readonly IQaRatingQueueService _qaRatingQueueService;
    private readonly IAgentRankingService _agentRankingService;

    public QaReviewService(
        ApplicationDbContext dbContext,
        IQaRatingQueueService qaRatingQueueService,
        IAgentRankingService agentRankingService)
    {
        _dbContext = dbContext;
        _qaRatingQueueService = qaRatingQueueService;
        _agentRankingService = agentRankingService;
    }

    public async Task<QaRatingPageData?> GetRatingPageAsync(
        int supportFaqId,
        string reviewerDisplayName,
        string? range,
        DateOnly? from,
        DateOnly? to,
        string? search,
        CancellationToken cancellationToken)
    {
        var supportFaq = await _dbContext.SupportFaqRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == supportFaqId, cancellationToken);

        if (supportFaq is null)
        {
            return null;
        }

        var session = await _dbContext.LiveAgentSessions
            .AsNoTracking()
            .Where(item => item.SupportFaqId == supportFaqId)
            .OrderByDescending(item => item.UpdatedAt)
            .ThenByDescending(item => item.LiveAgentSessionId)
            .FirstOrDefaultAsync(cancellationToken);

        ConsumerRef? consumer = null;
        if (session?.ConsumerId is int consumerId)
        {
            consumer = await _dbContext.Set<ConsumerRef>()
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.ConsumerId == consumerId, cancellationToken);
        }
        var customerName = ResolveCustomerName(consumer);

        var messages = await _dbContext.SupportMessages
            .AsNoTracking()
            .Where(item => item.ConversationId == supportFaqId)
            .OrderBy(item => item.CreatedAt)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);

        var review = await _dbContext.QaReviews
            .AsNoTracking()
            .Include(item => item.QuestionScores)
            .FirstOrDefaultAsync(item => item.SupportFaqId == supportFaqId, cancellationToken);
        var inlineCommentDraft = await _dbContext.QaReviewInlineCommentDrafts
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.SupportFaqId == supportFaqId, cancellationToken);
        var agentUserId = review?.AgentUserId
            ?? inlineCommentDraft?.AgentUserId
            ?? supportFaq.AgentId;
        var agentName = await ResolveAgentNameAsync(agentUserId, cancellationToken);
        var messageViewModels = messages
            .Select(item => new QaConversationMessageViewModel(
                item.Id.ToString(),
                string.Equals(item.SenderRole, "Consumer", StringComparison.OrdinalIgnoreCase) ? "user" : "agent",
                ResolveSenderDisplayName(item, customerName, agentName),
                item.CreatedAt.ToString("MMM dd, yyyy hh:mm tt"),
                item.MessageText,
                true))
            .ToList();

        if (messageViewModels.Count == 0 && !string.IsNullOrWhiteSpace(supportFaq.Question))
        {
            messageViewModels.Add(new QaConversationMessageViewModel(
                "question",
                "user",
                customerName,
                supportFaq.CreatedAt.ToString("MMM dd, yyyy hh:mm tt"),
                supportFaq.Question,
                true));
        }

        var queueState = await _qaRatingQueueService.GetQueueAsync(
            range,
            from,
            to,
            search,
            supportFaqId,
            cancellationToken);

        return new QaRatingPageData
        {
            SupportFaqId = supportFaqId,
            HasActiveTicket = true,
            PreviousSupportFaqId = queueState.PreviousSupportFaqId,
            NextSupportFaqId = queueState.NextSupportFaqId,
            AgentName = agentName,
            CustomerName = customerName,
            ConversationDateLabel = (supportFaq.EndTime ?? supportFaq.CreatedAt).ToString("MMM dd, yyyy"),
            Messages = messageViewModels,
            ReviewerDisplayName = string.IsNullOrWhiteSpace(review?.ReviewerName) ? reviewerDisplayName : review!.ReviewerName,
            InlineCommentsJson = ResolveInlineCommentsJson(review?.InlineCommentsJson, inlineCommentDraft?.InlineCommentsJson),
            Review = review,
            QueueCount = queueState.QueueCount,
            QueuePosition = queueState.QueuePosition,
            CurrentTicketInQueue = queueState.CurrentTicketInQueue,
            QueueRange = NormalizeRange(range),
            QueueFrom = from,
            QueueTo = to,
            QueueSearch = (search ?? string.Empty).Trim()
        };
    }

    public async Task<QaReviewMutationResponse> UpsertReviewAsync(
        int supportFaqId,
        int reviewerStaffId,
        string reviewerName,
        QaReviewUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var validationMessage = ValidateRequest(request);
        if (validationMessage is not null)
        {
            return new QaReviewMutationResponse(false, validationMessage);
        }

        var supportFaq = await _dbContext.SupportFaqRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == supportFaqId, cancellationToken);

        if (supportFaq is null)
        {
            return new QaReviewMutationResponse(false, "Support conversation not found.");
        }

        if (!supportFaq.AgentId.HasValue)
        {
            return new QaReviewMutationResponse(false, "Support conversation is not assigned to an agent.");
        }

        var agentUserId = supportFaq.AgentId.Value;

        var review = await _dbContext.QaReviews
            .Include(item => item.QuestionScores)
            .FirstOrDefaultAsync(item => item.SupportFaqId == supportFaqId, cancellationToken);

        var accuracyScores = request.AccuracyScores ?? Array.Empty<int>();
        var toneScores = request.ToneScores ?? Array.Empty<int>();
        var resolutionScores = request.ResolutionScores ?? Array.Empty<int>();
        var notes = (request.Notes ?? string.Empty).Trim();
        var inlineCommentThreads = request.InlineCommentThreads;
        var inlineCommentsJson = SerializeInlineComments(inlineCommentThreads);

        var nowUtc = DateTime.UtcNow;
        var accuracyAverage = Round2(accuracyScores.Average());
        var toneAverage = Round2(toneScores.Average());
        var resolutionAverage = Round2(resolutionScores.Average());
        var overallPercent = CalculateOverallPercent(accuracyAverage, toneAverage, resolutionAverage);

        if (review is null)
        {
            review = new QaReview
            {
                SupportFaqId = supportFaqId,
                AgentUserId = agentUserId,
                ReviewerStaffId = reviewerStaffId,
                ReviewerName = reviewerName,
                CreatedAtUtc = nowUtc
            };

            _dbContext.QaReviews.Add(review);
        }

        review.ReviewerStaffId = reviewerStaffId;
        review.ReviewerName = reviewerName;
        review.AgentUserId = agentUserId;
        review.Notes = notes;
        review.AccuracyAverage = accuracyAverage;
        review.ToneAverage = toneAverage;
        review.ResolutionAverage = resolutionAverage;
        review.OverallPercent = overallPercent;
        review.InlineCommentsJson = inlineCommentsJson;
        review.UpdatedAtUtc = nowUtc;

        if (request.SubmitToAgentNow)
        {
            review.SubmittedToAgent = true;
            review.SubmittedToAgentAtUtc = nowUtc;
        }

        if (review.QuestionScores.Count > 0)
        {
            _dbContext.QaReviewQuestionScores.RemoveRange(review.QuestionScores);
            review.QuestionScores.Clear();
        }

        foreach (var pair in BuildQuestionScorePairs(accuracyScores, toneScores, resolutionScores))
        {
            review.QuestionScores.Add(new QaReviewQuestionScore
            {
                QuestionKey = pair.Key,
                Score = pair.Value
            });
        }

        await UpsertInlineCommentDraftAsync(
            supportFaqId,
            agentUserId,
            reviewerStaffId,
            reviewerName,
            inlineCommentsJson,
            nowUtc,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _agentRankingService.RecomputeMonthAsync(AgentRankingMetricType.QaScore, review.CreatedAtUtc, cancellationToken);
        await _agentRankingService.RecomputeMonthAsync(AgentRankingMetricType.AverageHandlingTime, review.CreatedAtUtc, cancellationToken);
        return new QaReviewMutationResponse(true, "QA review saved.");
    }

    public async Task<QaReviewMutationResponse> UpsertInlineCommentsAsync(
        int supportFaqId,
        int reviewerStaffId,
        string reviewerName,
        QaInlineCommentsUpsertRequest request,
        CancellationToken cancellationToken)
    {
        var validationMessage = ValidateInlineCommentsRequest(request);
        if (validationMessage is not null)
        {
            return new QaReviewMutationResponse(false, validationMessage);
        }

        var supportFaq = await _dbContext.SupportFaqRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == supportFaqId, cancellationToken);

        if (supportFaq is null)
        {
            return new QaReviewMutationResponse(false, "Support conversation not found.");
        }

        if (!supportFaq.AgentId.HasValue)
        {
            return new QaReviewMutationResponse(false, "Support conversation is not assigned to an agent.");
        }

        var inlineCommentsJson = SerializeInlineComments(request.InlineCommentThreads);
        var nowUtc = DateTime.UtcNow;

        await UpsertInlineCommentDraftAsync(
            supportFaqId,
            supportFaq.AgentId.Value,
            reviewerStaffId,
            reviewerName,
            inlineCommentsJson,
            nowUtc,
            cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new QaReviewMutationResponse(true, "QA conversation comments saved.");
    }

    public async Task<QaReviewMutationResponse> SubmitToAgentAsync(
        int supportFaqId,
        int reviewerStaffId,
        string reviewerName,
        CancellationToken cancellationToken)
    {
        var review = await _dbContext.QaReviews
            .FirstOrDefaultAsync(item => item.SupportFaqId == supportFaqId, cancellationToken);

        if (review is null)
        {
            return new QaReviewMutationResponse(false, "QA review not found.");
        }

        review.ReviewerStaffId = reviewerStaffId;
        review.ReviewerName = reviewerName;
        review.SubmittedToAgent = true;
        review.SubmittedToAgentAtUtc = DateTime.UtcNow;
        review.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return new QaReviewMutationResponse(true, "QA review submitted to agent.");
    }

    private static string? ValidateRequest(QaReviewUpsertRequest? request)
    {
        if (request is null)
        {
            return "Invalid QA review payload.";
        }

        if (!IsValidScoreSet(request.AccuracyScores)
            || !IsValidScoreSet(request.ToneScores)
            || !IsValidScoreSet(request.ResolutionScores))
        {
            return "All 9 questions must be scored from 1 to 5.";
        }

        if ((request.Notes ?? string.Empty).Length > 4000)
        {
            return "QA notes exceed the maximum length.";
        }

        return null;
    }

    private static string? ValidateInlineCommentsRequest(QaInlineCommentsUpsertRequest? request)
    {
        if (request is null)
        {
            return "Invalid QA inline comments payload.";
        }

        return null;
    }

    private static bool IsValidScoreSet(IReadOnlyList<int>? scores)
    {
        return scores is not null
            && scores.Count == 3
            && scores.All(score => score is >= 1 and <= 5);
    }

    private static IEnumerable<KeyValuePair<string, int>> BuildQuestionScorePairs(
        IReadOnlyList<int> accuracyScores,
        IReadOnlyList<int> toneScores,
        IReadOnlyList<int> resolutionScores)
    {
        for (var index = 0; index < 3; index++)
        {
            yield return new KeyValuePair<string, int>(AccuracyKeys[index], accuracyScores[index]);
            yield return new KeyValuePair<string, int>(ToneKeys[index], toneScores[index]);
            yield return new KeyValuePair<string, int>(ResolutionKeys[index], resolutionScores[index]);
        }
    }

    private static decimal Round2(double value)
    {
        return Math.Round((decimal)value, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal CalculateOverallPercent(decimal accuracyAverage, decimal toneAverage, decimal resolutionAverage)
    {
        var accuracyPoints = (accuracyAverage / 5m) * 35m;
        var tonePoints = (toneAverage / 5m) * 35m;
        var resolutionPoints = (resolutionAverage / 5m) * 30m;
        return Math.Round(accuracyPoints + tonePoints + resolutionPoints, 2, MidpointRounding.AwayFromZero);
    }

    private static string SerializeInlineComments(IReadOnlyDictionary<string, List<QaInlineCommentEntry>>? inlineCommentThreads)
    {
        if (inlineCommentThreads is null || inlineCommentThreads.Count == 0)
        {
            return "{}";
        }

        var normalized = inlineCommentThreads
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Key))
            .Select(pair => new
            {
                MessageId = pair.Key,
                Entries = (pair.Value ?? [])
                    .Where(entry => entry is not null && !string.IsNullOrWhiteSpace(entry.Comment))
                    .Select(entry => new QaInlineCommentEntry
                    {
                        By = (entry!.By ?? string.Empty).Trim(),
                        On = (entry.On ?? string.Empty).Trim(),
                        Comment = (entry.Comment ?? string.Empty).Trim(),
                        Images = (entry.Images ?? [])
                            .Where(image => !string.IsNullOrWhiteSpace(image))
                            .Select(image => image.Trim())
                            .ToList()
                    })
                    .ToList()
            })
            .Where(pair => pair.Entries.Count > 0)
            .ToDictionary(pair => pair.MessageId, pair => pair.Entries, StringComparer.Ordinal);

        return normalized.Count == 0
            ? "{}"
            : JsonSerializer.Serialize(normalized, JsonOptions);
    }

    private async Task UpsertInlineCommentDraftAsync(
        int supportFaqId,
        int agentUserId,
        int reviewerStaffId,
        string reviewerName,
        string inlineCommentsJson,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var draft = await _dbContext.QaReviewInlineCommentDrafts
            .FirstOrDefaultAsync(item => item.SupportFaqId == supportFaqId, cancellationToken);

        if (draft is null)
        {
            draft = new QaReviewInlineCommentDraft
            {
                SupportFaqId = supportFaqId,
                CreatedAtUtc = nowUtc
            };

            _dbContext.QaReviewInlineCommentDrafts.Add(draft);
        }

        draft.AgentUserId = agentUserId;
        draft.UpdatedByStaffId = reviewerStaffId;
        draft.UpdatedByName = reviewerName;
        draft.InlineCommentsJson = inlineCommentsJson;
        draft.UpdatedAtUtc = nowUtc;
    }

    private static string ResolveInlineCommentsJson(string? reviewInlineCommentsJson, string? draftInlineCommentsJson)
    {
        if (!IsEmptyInlineCommentsJson(reviewInlineCommentsJson))
        {
            return reviewInlineCommentsJson!;
        }

        if (!IsEmptyInlineCommentsJson(draftInlineCommentsJson))
        {
            return draftInlineCommentsJson!;
        }

        return "{}";
    }

    private static bool IsEmptyInlineCommentsJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        return string.Equals(value.Trim(), "{}", StringComparison.Ordinal);
    }

    private async Task<string> ResolveAgentNameAsync(int? agentUserId, CancellationToken cancellationToken)
    {
        if (agentUserId is not int userId)
        {
            return "Unassigned";
        }

        var agentName = await _dbContext.SupportAgents
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.AgentStatus == "available")
            .ThenByDescending(item => item.ChatId)
            .Select(item => item.AgentName)
            .FirstOrDefaultAsync(cancellationToken);

        return string.IsNullOrWhiteSpace(agentName) ? "Unknown Agent" : agentName.Trim();
    }

    private static string ResolveCustomerName(ConsumerRef? consumer)
    {
        if (consumer is null)
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

    private static string ResolveSenderDisplayName(SupportMessage message, string customerName, string agentName)
    {
        if (string.Equals(message.SenderRole, "Consumer", StringComparison.OrdinalIgnoreCase))
        {
            return customerName;
        }

        return message.SenderId == 0 ? "Support" : $"Agent {agentName}";
    }

    private static string NormalizeRange(string? range)
    {
        var normalized = (range ?? "custom").Trim().ToLowerInvariant();
        return normalized is "today" or "last7" or "last30" ? normalized : "custom";
    }
}
