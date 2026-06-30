using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NextHorizon.Data;
using NextHorizon.Models.AgentDashboard;
using NextHorizon.Models.HelpCenter;
using NextHorizon.Models.QA;
using MyAspNetApp.Models.Messaging;

namespace NextHorizon.Services;

public sealed class QaReviewService : IQaReviewService
{
    private const string QaScoreNotificationCategory = "QAScore";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly ApplicationDbContext _dbContext;
    private readonly INotificationService _notificationService;
    private readonly IQaRatingQueueService _qaRatingQueueService;
    private readonly IAgentRankingService _agentRankingService;

    public QaReviewService(
        ApplicationDbContext dbContext,
        INotificationService notificationService,
        IQaRatingQueueService qaRatingQueueService,
        IAgentRankingService agentRankingService)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
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

        var messages = await _dbContext.SupportMessages
            .AsNoTracking()
            .Where(item => item.ConversationId == supportFaqId)
            .OrderBy(item => item.CreatedAt)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);
        var concernFrom = QaConcernFormatting.NormalizeConcernFrom(supportFaq.UserType);
        var participantName = ResolveParticipantName(concernFrom, consumer);

        var review = await _dbContext.QaReviews
            .AsNoTracking()
            .Include(item => item.QuestionScores)
            .Include(item => item.EvaluationTemplate!)
                .ThenInclude(item => item.Categories.OrderBy(category => category.DisplayOrder))
                    .ThenInclude(item => item.Questions.OrderBy(question => question.DisplayOrder))
            .FirstOrDefaultAsync(item => item.SupportFaqId == supportFaqId, cancellationToken);
        var inlineCommentDraft = await _dbContext.QaReviewInlineCommentDrafts
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.SupportFaqId == supportFaqId, cancellationToken);
        var agentUserId = review?.AgentUserId
            ?? inlineCommentDraft?.AgentUserId
            ?? supportFaq.AgentId;
        var agentName = await ResolveAgentNameAsync(agentUserId, cancellationToken);
        var messageViewModels = await BuildConversationMessagesAsync(
            supportFaq,
            session,
            consumer,
            participantName,
            agentName,
            messages,
            cancellationToken);

        var queueState = await _qaRatingQueueService.GetQueueAsync(
            range,
            from,
            to,
            search,
            supportFaqId,
            cancellationToken);

        var template = review?.EvaluationTemplate;
        if (template is null)
        {
            template = await _dbContext.QaEvaluationTemplates
                .AsNoTracking()
                .Where(item => item.IsActive)
                .Include(item => item.Categories.OrderBy(category => category.DisplayOrder))
                    .ThenInclude(item => item.Questions.OrderBy(question => question.DisplayOrder))
                .OrderByDescending(item => item.VersionNumber)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var scoreMap = review?.QuestionScores
            .Where(item => item.QaEvaluationQuestionId.HasValue)
            .ToDictionary(item => item.QaEvaluationQuestionId!.Value, item => item.Score)
            ?? new Dictionary<int, int>();

        return new QaRatingPageData
        {
            SupportFaqId = supportFaqId,
            HasActiveTicket = true,
            PreviousSupportFaqId = queueState.PreviousSupportFaqId,
            NextSupportFaqId = queueState.NextSupportFaqId,
            AgentName = agentName,
            ConcernFrom = concernFrom,
            ParticipantName = participantName,
            CustomerName = participantName,
            ConversationDateLabel = (supportFaq.EndTime ?? supportFaq.CreatedAt).ToString("MMM dd, yyyy"),
            Messages = messageViewModels,
            ReviewerDisplayName = string.IsNullOrWhiteSpace(review?.ReviewerName) ? reviewerDisplayName : review!.ReviewerName,
            InlineCommentsJson = ResolveInlineCommentsJson(review?.InlineCommentsJson, inlineCommentDraft?.InlineCommentsJson),
            Review = review,
            EvaluationTemplate = template is null ? null : QaEvaluationService.MapTemplate(template, scoreMap),
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
        if (request is null)
        {
            return new QaReviewMutationResponse(false, "Invalid QA review payload.");
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
            .Include(item => item.CategoryScores)
            .Include(item => item.QuestionScores)
            .FirstOrDefaultAsync(item => item.SupportFaqId == supportFaqId, cancellationToken);

        var templateId = review?.QaEvaluationTemplateId ?? request.TemplateId;
        var template = await _dbContext.QaEvaluationTemplates
            .AsNoTracking()
            .Where(item => item.QaEvaluationTemplateId == templateId)
            .Include(item => item.Categories.OrderBy(category => category.DisplayOrder))
                .ThenInclude(item => item.Questions.OrderBy(question => question.DisplayOrder))
            .FirstOrDefaultAsync(cancellationToken);

        if (template is null)
        {
            return new QaReviewMutationResponse(false, "QA evaluation template not found.");
        }

        var validationMessage = ValidateRequest(request, template);
        if (validationMessage is not null)
        {
            return new QaReviewMutationResponse(false, validationMessage);
        }

        var scoreMap = request.Scores
            .ToDictionary(item => item.QuestionId, item => item.Score);
        var notes = (request.Notes ?? string.Empty).Trim();
        var inlineCommentThreads = request.InlineCommentThreads;
        var inlineCommentsJson = SerializeInlineComments(inlineCommentThreads);

        var orderedCategories = template.Categories
            .OrderBy(category => category.DisplayOrder)
            .ToList();
        var categoryAverages = orderedCategories
            .Select(category =>
            {
                var questionScores = category.Questions
                    .OrderBy(question => question.DisplayOrder)
                    .Select(question => scoreMap[question.QaEvaluationQuestionId])
                    .ToArray();
                return Round2(questionScores.Average());
            })
            .ToList();

        var overallPercent = Math.Round(
            orderedCategories.Select((category, index) => (categoryAverages[index] / 5m) * category.WeightPercent).Sum(),
            2,
            MidpointRounding.AwayFromZero);

        var nowUtc = DateTime.UtcNow;
        var wasAlreadySubmittedToAgent = review?.SubmittedToAgent ?? false;

        if (review is null)
        {
            review = new QaReview
            {
                SupportFaqId = supportFaqId,
                AgentUserId = agentUserId,
                ReviewerStaffId = reviewerStaffId,
                ReviewerName = reviewerName,
                QaEvaluationTemplateId = template.QaEvaluationTemplateId,
                CreatedAtUtc = nowUtc
            };

            _dbContext.QaReviews.Add(review);
        }

        review.ReviewerStaffId = reviewerStaffId;
        review.ReviewerName = reviewerName;
        review.AgentUserId = agentUserId;
        review.QaEvaluationTemplateId = template.QaEvaluationTemplateId;
        review.Notes = notes;
        review.OverallPercent = overallPercent;
        review.InlineCommentsJson = inlineCommentsJson;
        review.UpdatedAtUtc = nowUtc;

        if (request.SubmitToAgentNow && !wasAlreadySubmittedToAgent)
        {
            review.SubmittedToAgent = true;
            review.SubmittedToAgentAtUtc = nowUtc;
        }

        if (review.QuestionScores.Count > 0)
        {
            _dbContext.QaReviewQuestionScores.RemoveRange(review.QuestionScores);
            review.QuestionScores.Clear();
        }

        if (review.CategoryScores.Count > 0)
        {
            _dbContext.QaReviewCategoryScores.RemoveRange(review.CategoryScores);
            review.CategoryScores.Clear();
        }

        for (var categoryIndex = 0; categoryIndex < orderedCategories.Count; categoryIndex++)
        {
            var category = orderedCategories[categoryIndex];
            review.CategoryScores.Add(new QaReviewCategoryScore
            {
                QaEvaluationCategoryId = category.QaEvaluationCategoryId,
                CategoryNameSnapshot = category.Name,
                WeightPercentSnapshot = category.WeightPercent,
                AverageScore = categoryAverages[categoryIndex],
                WeightedPoints = Math.Round((categoryAverages[categoryIndex] / 5m) * category.WeightPercent, 2, MidpointRounding.AwayFromZero),
                DisplayOrder = category.DisplayOrder
            });

            foreach (var question in category.Questions.OrderBy(item => item.DisplayOrder))
            {
                review.QuestionScores.Add(new QaReviewQuestionScore
                {
                    QaEvaluationQuestionId = question.QaEvaluationQuestionId,
                    QuestionKey = question.QuestionKey,
                    CategoryNameSnapshot = category.Name,
                    QuestionTextSnapshot = question.Prompt,
                    Score = scoreMap[question.QaEvaluationQuestionId]
                });
            }
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

        if (request.SubmitToAgentNow && !wasAlreadySubmittedToAgent)
        {
            await CreateQaScoreNotificationAsync(review.SupportFaqId, review.AgentUserId, reviewerName, review.OverallPercent, cancellationToken);
        }

        await _agentRankingService.RecomputeMonthAsync(AgentRankingMetricType.QaScore, review.CreatedAtUtc, cancellationToken);
        await _agentRankingService.RecomputeMonthAsync(AgentRankingMetricType.AverageHandlingTime, review.CreatedAtUtc, cancellationToken);
        var saveMessage = request.SubmitToAgentNow && review.SubmittedToAgent
            ? "QA review saved and submitted to agent."
            : "QA review saved.";

        return new QaReviewMutationResponse(
            true,
            saveMessage,
            review.SubmittedToAgent,
            review.SubmittedToAgentAtUtc?.ToString("MMM dd, yyyy hh:mm tt") ?? string.Empty,
            (double)Math.Round(review.OverallPercent, 1, MidpointRounding.AwayFromZero));
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

        var wasAlreadySubmittedToAgent = review.SubmittedToAgent;
        var nowUtc = DateTime.UtcNow;
        review.ReviewerStaffId = reviewerStaffId;
        review.ReviewerName = reviewerName;
        if (!wasAlreadySubmittedToAgent)
        {
            review.SubmittedToAgent = true;
            review.SubmittedToAgentAtUtc = nowUtc;
        }
        review.UpdatedAtUtc = nowUtc;

        await _dbContext.SaveChangesAsync(cancellationToken);

        if (!wasAlreadySubmittedToAgent)
        {
            await CreateQaScoreNotificationAsync(review.SupportFaqId, review.AgentUserId, reviewerName, review.OverallPercent, cancellationToken);
        }

        var submitMessage = wasAlreadySubmittedToAgent
            ? "QA review already submitted to agent."
            : "QA review submitted to agent.";

        return new QaReviewMutationResponse(
            true,
            submitMessage,
            review.SubmittedToAgent,
            review.SubmittedToAgentAtUtc?.ToString("MMM dd, yyyy hh:mm tt") ?? string.Empty,
            (double)Math.Round(review.OverallPercent, 1, MidpointRounding.AwayFromZero));
    }

    private static string? ValidateRequest(QaReviewUpsertRequest request, QaEvaluationTemplate template)
    {
        if ((request.Notes ?? string.Empty).Length > 4000)
        {
            return "QA notes exceed the maximum length.";
        }

        var questions = template.Categories
            .SelectMany(category => category.Questions)
            .ToList();
        if (questions.Count == 0)
        {
            return "QA evaluation template is missing questions.";
        }

        if (request.Scores is null || request.Scores.Count != questions.Count)
        {
            return $"All {questions.Count} questions must be scored from 1 to 5.";
        }

        var validQuestionIds = questions
            .Select(question => question.QaEvaluationQuestionId)
            .ToHashSet();
        var distinctQuestionIds = new HashSet<int>();

        foreach (var score in request.Scores)
        {
            if (score.QuestionId <= 0 || score.Score is < 1 or > 5)
            {
                return $"All {questions.Count} questions must be scored from 1 to 5.";
            }

            if (!validQuestionIds.Contains(score.QuestionId) || !distinctQuestionIds.Add(score.QuestionId))
            {
                return "Invalid QA question payload.";
            }
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

    private static decimal Round2(double value)
    {
        return Math.Round((decimal)value, 2, MidpointRounding.AwayFromZero);
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

    private async Task CreateQaScoreNotificationAsync(
        int supportFaqId,
        int agentUserId,
        string reviewerName,
        decimal overallPercent,
        CancellationToken cancellationToken)
    {
        var message = BuildQaScoreNotificationMessage(supportFaqId, reviewerName, overallPercent);
        await _notificationService.NotifyUserAsync(
            agentUserId,
            message,
            QaScoreNotificationCategory,
            orderId: null,
            cancellationToken);
    }

    private static string BuildQaScoreNotificationMessage(int supportFaqId, string reviewerName, decimal overallPercent)
    {
        var roundedScore = Math.Round(overallPercent, 1, MidpointRounding.AwayFromZero);
        var reviewerLabel = string.IsNullOrWhiteSpace(reviewerName)
            ? "QA"
            : reviewerName.Trim();
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{reviewerLabel} has evaluated your QA score for ticket #{supportFaqId} as {roundedScore:0.0}/100.");
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

    private async Task<List<QaConversationMessageViewModel>> BuildConversationMessagesAsync(
        SupportFaqRecord supportFaq,
        LiveAgentSession? session,
        ConsumerRef? consumer,
        string participantName,
        string agentName,
        IReadOnlyList<SupportMessage> supportMessages,
        CancellationToken cancellationToken)
    {
        var seeds = supportMessages
            .Where(item => !string.IsNullOrWhiteSpace(item.MessageText))
            .Select(item => new QaConversationSeed(
                item.Id.ToString(),
                ResolveSupportSenderCssClass(item.SenderRole),
                ResolveSenderDisplayName(item, participantName, agentName),
                item.CreatedAt,
                item.MessageText.Trim(),
                0))
            .ToList();

        var hasCustomerMessage = seeds.Any(item => string.Equals(item.SenderCssClass, "user", StringComparison.Ordinal));

        if (!hasCustomerMessage && !string.IsNullOrWhiteSpace(supportFaq.Question))
        {
            seeds.Add(new QaConversationSeed(
                "faq-question",
                "user",
                participantName,
                supportFaq.CreatedAt,
                supportFaq.Question.Trim(),
                -1));
            hasCustomerMessage = true;
        }

        if (!hasCustomerMessage)
        {
            var supplementalMessages = await LoadSupplementalConversationMessagesAsync(session, consumer, cancellationToken);
            if (supplementalMessages.Count > 0)
            {
                var supplementalSeeds = supplementalMessages
                    .Where(item => !string.IsNullOrWhiteSpace(item.Body))
                    .Select(item => MapSupplementalMessage(item, session!, consumer!, participantName))
                    .Where(item => item is not null)
                    .Select(item => item!)
                    .ToList();

                foreach (var supplementalSeed in supplementalSeeds)
                {
                    if (seeds.Any(existing => IsDuplicateMessage(existing, supplementalSeed)))
                    {
                        continue;
                    }

                    seeds.Add(supplementalSeed);
                }
            }
        }

        return seeds
            .OrderBy(item => item.TimestampUtc)
            .ThenBy(item => item.SourceRank)
            .ThenBy(item => item.MessageId, StringComparer.Ordinal)
            .Select(item => new QaConversationMessageViewModel(
                item.MessageId,
                item.SenderCssClass,
                item.SenderDisplayName,
                item.TimestampUtc.ToString("MMM dd, yyyy hh:mm tt"),
                item.MessageText,
                true))
            .ToList();
    }

    private async Task<List<ConversationMessage>> LoadSupplementalConversationMessagesAsync(
        LiveAgentSession? session,
        ConsumerRef? consumer,
        CancellationToken cancellationToken)
    {
        if (session is null || consumer is null || session.UserId <= 0 || consumer.UserId <= 0)
        {
            return [];
        }

        var candidateConversationIds = await _dbContext.MessageConversations
            .AsNoTracking()
            .Where(item =>
                (item.BuyerUserId == consumer.UserId && item.SellerUserId == session.UserId)
                || (item.BuyerUserId == session.UserId && item.SellerUserId == consumer.UserId))
            .Select(item => item.ConversationId)
            .ToListAsync(cancellationToken);

        if (candidateConversationIds.Count != 1)
        {
            return [];
        }

        var conversationId = candidateConversationIds[0];
        return await _dbContext.ConversationMessages
            .AsNoTracking()
            .Where(item => item.ConversationId == conversationId && !item.IsDeleted)
            .OrderBy(item => item.SentAt)
            .ThenBy(item => item.MessageId)
            .ToListAsync(cancellationToken);
    }

    private static string ResolveParticipantName(string concernFrom, ConsumerRef? consumer)
    {
        if (string.Equals(concernFrom, "Seller", StringComparison.OrdinalIgnoreCase))
        {
            return "Seller";
        }

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

    private static QaConversationSeed? MapSupplementalMessage(
        ConversationMessage message,
        LiveAgentSession session,
        ConsumerRef consumer,
        string participantName)
    {
        var senderCssClass = ResolveSupplementalSenderCssClass(message.SenderUserId, session, consumer);
        if (senderCssClass is null)
        {
            return null;
        }

        return new QaConversationSeed(
            $"commerce-{message.ConversationId}-{message.MessageId}",
            senderCssClass,
            string.Equals(senderCssClass, "user", StringComparison.Ordinal) ? participantName : "Seller",
            message.SentAt,
            message.Body.Trim(),
            1);
    }

    private static bool IsDuplicateMessage(QaConversationSeed left, QaConversationSeed right)
    {
        return string.Equals(left.SenderCssClass, right.SenderCssClass, StringComparison.Ordinal)
            && string.Equals(left.MessageText, right.MessageText, StringComparison.Ordinal)
            && left.TimestampUtc == right.TimestampUtc;
    }

    private static string ResolveSupportSenderCssClass(string? senderRole)
    {
        return IsConcernParticipantSenderRole(senderRole) ? "user" : "agent";
    }

    private static bool IsConcernParticipantSenderRole(string? senderRole)
    {
        var normalized = (senderRole ?? string.Empty).Trim();
        return normalized.Equals("Consumer", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Customer", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Buyer", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("User", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Seller", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ResolveSupplementalSenderCssClass(int senderUserId, LiveAgentSession session, ConsumerRef consumer)
    {
        if (senderUserId == consumer.UserId)
        {
            return "user";
        }

        if (senderUserId == session.UserId)
        {
            return "agent";
        }

        return null;
    }

    private static string ResolveSenderDisplayName(SupportMessage message, string participantName, string agentName)
    {
        if (IsConcernParticipantSenderRole(message.SenderRole))
        {
            return participantName;
        }

        return message.SenderId == 0 ? "Support" : $"Agent {agentName}";
    }

    private static string NormalizeRange(string? range)
    {
        var normalized = (range ?? "custom").Trim().ToLowerInvariant();
        return normalized is "today" or "last7" or "last30" ? normalized : "custom";
    }

    private sealed record QaConversationSeed(
        string MessageId,
        string SenderCssClass,
        string SenderDisplayName,
        DateTime TimestampUtc,
        string MessageText,
        int SourceRank);
}
