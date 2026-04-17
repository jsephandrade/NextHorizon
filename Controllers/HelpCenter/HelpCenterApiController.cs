using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using NextHorizon.Data;
using NextHorizon.Models.HelpCenter;
using NextHorizon.Modules.MemberTracker.Security;
using NextHorizon.Security;

namespace NextHorizon.Controllers;

[ApiController]
[Route("api/help")]
public sealed class HelpCenterApiController : ControllerBase
{
    private const int MaxSearchResults = 10;
    private const string ConsumerUserType = "Consumer";
    private const string ActiveStatus = "Active";
    private const string DefaultCategoryIcon = "fa-solid fa-circle-question";
    private const string ContactCategorySlug = "contact";
    private const string ContactCategoryTitle = "Contact Support";
    private const string ContactCategoryDescription = "Reach support directly or submit a ticket for help.";
    private const string ContactCategoryIcon = "fa-solid fa-life-ring";
    private const string SupportAgentRole = "Agent";
    private const string ConsumerSenderRole = "Consumer";
    private const string LiveAgentQueueWelcomeMessage = "Thank you for reaching out regarding your concern. An agent will be assigned to you shortly. There are currently 3 people ahead of you in the queue.";
    private const string LiveAgentWaitingReplyMessage = "An agent will assist you shortly.";
    private static readonly TimeSpan LiveAgentInactivityTimeout = TimeSpan.FromMinutes(5);

    private readonly ApplicationDbContext _dbContext;
    private readonly IAuthenticatedUserContextService _authenticatedUserContextService;

    public HelpCenterApiController(
        ApplicationDbContext dbContext,
        IAuthenticatedUserContextService authenticatedUserContextService)
    {
        _dbContext = dbContext;
        _authenticatedUserContextService = authenticatedUserContextService;
    }

    [HttpGet("home")]
    public async Task<ActionResult<HelpHomeResponseDto>> GetHome(CancellationToken cancellationToken)
    {
        var categories = await GetCategorySummariesAsync(cancellationToken);

        var featuredFaqs = await QueryVisibleFaqs()
            .OrderBy(faq => faq.Category)
            .ThenBy(faq => faq.FaqId)
            .Select(faq => new
            {
                faq.FaqId,
                faq.Question,
                faq.Answer,
                faq.Category,
            })
            .Take(4)
            .ToListAsync(cancellationToken);

        var featured = featuredFaqs
            .Select(faq => new HelpFeaturedFaqDto(
                faq.FaqId,
                faq.Question,
                faq.Answer,
                NormalizeSlug(faq.Category),
                faq.Category.Trim()))
            .ToList();

        return Ok(new HelpHomeResponseDto(categories, featured));
    }

    [HttpGet("categories")]
    public async Task<ActionResult<IReadOnlyList<HelpCategorySummaryDto>>> GetCategories(CancellationToken cancellationToken)
    {
        var categories = await GetCategorySummariesAsync(cancellationToken);
        return Ok(categories);
    }

    [HttpGet("categories/{slug}")]
    public async Task<ActionResult<HelpCategoryDetailDto>> GetCategory(string slug, CancellationToken cancellationToken)
    {
        var normalizedSlug = NormalizeSlug(slug);
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return NotFound();
        }

        if (string.Equals(normalizedSlug, ContactCategorySlug, StringComparison.Ordinal))
        {
            return Ok(new HelpCategoryDetailDto(
                ContactCategorySlug,
                ContactCategoryTitle,
                ContactCategoryDescription,
                ContactCategoryIcon,
                int.MaxValue,
                Array.Empty<HelpFaqDto>()));
        }

        var categoryName = await ResolveCategoryNameAsync(normalizedSlug, cancellationToken);
        if (categoryName is null)
        {
            return NotFound();
        }

        var category = CreateCategorySummary(categoryName, await GetCategoryDisplayOrderAsync(categoryName, cancellationToken));

        var faqs = await QueryVisibleFaqs()
            .Where(faq => faq.Category.Trim() == categoryName)
            .OrderBy(faq => faq.FaqId)
            .Select(faq => new HelpFaqDto(faq.FaqId, faq.Question, faq.Answer, faq.FaqId))
            .ToListAsync(cancellationToken);

        return Ok(new HelpCategoryDetailDto(
            category.Slug,
            category.Title,
            category.Description,
            category.IconKey,
            category.DisplayOrder,
            faqs));
    }

    [HttpGet("search")]
    [EnableRateLimiting("help-search")]
    public async Task<ActionResult<IReadOnlyList<HelpSearchResultDto>>> Search([FromQuery] string? query, CancellationToken cancellationToken)
    {
        var normalizedQuery = query?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return Ok(Array.Empty<HelpSearchResultDto>());
        }

        var pattern = $"%{normalizedQuery}%";

        var results = await QueryVisibleFaqs()
            .Where(faq => EF.Functions.Like(faq.Question, pattern)
                || EF.Functions.Like(faq.Answer, pattern)
                || EF.Functions.Like(faq.Category, pattern))
            .OrderBy(faq => faq.Category)
            .ThenBy(faq => faq.FaqId)
            .Select(faq => new
            {
                faq.FaqId,
                faq.Question,
                faq.Answer,
                faq.Category,
            })
            .Take(MaxSearchResults)
            .ToListAsync(cancellationToken);

        return Ok(results.Select(result => new HelpSearchResultDto(
            result.FaqId,
            result.Question,
            result.Answer,
            NormalizeSlug(result.Category),
            result.Category.Trim())).ToList());
    }

    [HttpGet("contact")]
    public async Task<ActionResult<HelpContactResponseDto>> GetContact(CancellationToken cancellationToken)
    {
        var channels = await _dbContext.SupportContactChannels
            .AsNoTracking()
            .Where(channel => channel.IsActive)
            .OrderBy(channel => channel.DisplayOrder)
            .Select(channel => new SupportContactDto(
                channel.ChannelType,
                channel.Label,
                channel.Value,
                channel.DisplayText,
                channel.ActionHref,
                channel.DisplayOrder))
            .ToListAsync(cancellationToken);

        return Ok(new HelpContactResponseDto(channels));
    }

    [Authorize]
    [HttpPost("tickets")]
    [EnableRateLimiting("help-ticket-create")]
    [ConditionalValidateAntiForgeryToken]
    public async Task<ActionResult<CreateSupportTicketResponse>> CreateTicket([FromBody] CreateSupportTicketRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await _authenticatedUserContextService.GetCurrentAsync(User, cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized();
        }

        var categorySlug = NormalizeSlug(request.CategorySlug);
        var categoryName = string.IsNullOrWhiteSpace(categorySlug)
            ? null
            : await ResolveCategoryNameAsync(categorySlug, cancellationToken);

        if (!string.IsNullOrWhiteSpace(categorySlug) && categoryName is null)
        {
            return BadRequest("CategorySlug must reference an active consumer FAQ category.");
        }

        var ticket = new SupportTicket
        {
            ReferenceCode = CreateReferenceCode(),
            UserId = currentUser.UserId,
            ConsumerId = currentUser.ConsumerId,
            FaqCategory = categoryName,
            Subject = request.Subject.Trim(),
            Body = request.Body.Trim(),
            Status = SupportTicketStatus.Open,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _dbContext.SupportTickets.Add(ticket);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            new CreateSupportTicketResponse(
                ticket.SupportTicketId,
                ticket.ReferenceCode,
                ticket.Status.ToString(),
                ticket.CreatedAt));
    }

    [Authorize]
    [HttpGet("live-agent/sessions/current")]
    public async Task<ActionResult<LiveAgentSessionResponse>> GetCurrentLiveAgentSession(CancellationToken cancellationToken)
    {
        var currentUser = await _authenticatedUserContextService.GetCurrentAsync(User, cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized();
        }

        var session = await NormalizeCurrentLiveAgentSessionAsync(currentUser.UserId, cancellationToken);
        if (session is null)
        {
            return NoContent();
        }

        return Ok(await CreateLiveAgentSessionResponseAsync(session, cancellationToken));
    }

    [Authorize]
    [HttpGet("live-agent/sessions/last-ended")]
    public async Task<ActionResult<LastEndedLiveAgentNoticeResponse>> GetLastEndedLiveAgentNotice(CancellationToken cancellationToken)
    {
        var currentUser = await _authenticatedUserContextService.GetCurrentAsync(User, cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized();
        }

        var activeSession = await NormalizeCurrentLiveAgentSessionAsync(currentUser.UserId, cancellationToken);
        if (activeSession is not null)
        {
            return NoContent();
        }

        var notice = await CreateLastEndedLiveAgentNoticeAsync(currentUser.UserId, cancellationToken);
        if (notice is null)
        {
            return NoContent();
        }

        return Ok(notice);
    }

    [Authorize]
    [HttpPost("live-agent/sessions")]
    [EnableRateLimiting("help-ticket-create")]
    [ConditionalValidateAntiForgeryToken]
    public async Task<ActionResult<LiveAgentSessionResponse>> CreateLiveAgentSession([FromBody] CreateLiveAgentSessionRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await _authenticatedUserContextService.GetCurrentAsync(User, cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized();
        }

        var categorySlug = NormalizeSlug(request.CategorySlug);
        var categoryName = await ResolveCategoryNameAsync(categorySlug, cancellationToken);
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            return BadRequest("CategorySlug must reference an active consumer FAQ category.");
        }

        var activeSession = await NormalizeCurrentLiveAgentSessionAsync(currentUser.UserId, cancellationToken);
        if (activeSession is not null)
        {
            return Ok(await CreateLiveAgentSessionResponseAsync(activeSession, cancellationToken));
        }

        var timestamp = DateTime.UtcNow;
        var supportFaqRecord = new SupportFaqRecord
        {
            Category = categoryName,
            Question = string.Empty,
            Status = "Waiting",
            DurationMinutes = 0,
            UserType = ConsumerUserType,
            AgentId = null,
            CreatedAt = timestamp,
            StartTime = null,
            EndTime = null,
        };

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        _dbContext.SupportFaqRecords.Add(supportFaqRecord);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _dbContext.SupportMessages.Add(new SupportMessage
        {
            ConversationId = supportFaqRecord.Id,
            SenderId = 0,
            SenderRole = SupportAgentRole,
            MessageText = LiveAgentQueueWelcomeMessage,
            CreatedAt = timestamp,
        });

        var session = new LiveAgentSession
        {
            SupportFaqId = supportFaqRecord.Id,
            UserId = currentUser.UserId,
            ConsumerId = currentUser.ConsumerId,
            CategorySlug = NormalizeSlug(categoryName),
            CategoryTitle = categoryName,
            FirstQuestion = string.Empty,
            Status = LiveAgentSessionStatus.Waiting,
            CreatedAt = timestamp,
            UpdatedAt = timestamp,
        };

        _dbContext.LiveAgentSessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return StatusCode(
            StatusCodes.Status201Created,
            await CreateLiveAgentSessionResponseAsync(session, cancellationToken));
    }

    [Authorize]
    [HttpPost("live-agent/sessions/{sessionId:int}/question")]
    [EnableRateLimiting("help-ticket-create")]
    [ConditionalValidateAntiForgeryToken]
    public async Task<ActionResult<CaptureLiveAgentQuestionResponse>> CaptureLiveAgentQuestion(int sessionId, [FromBody] CaptureLiveAgentQuestionRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await _authenticatedUserContextService.GetCurrentAsync(User, cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized();
        }

        var message = request.Message.Trim();
        if (string.IsNullOrWhiteSpace(message))
        {
            return BadRequest("Message is required.");
        }

        await NormalizeCurrentLiveAgentSessionAsync(currentUser.UserId, cancellationToken);

        var session = await _dbContext.LiveAgentSessions
            .FirstOrDefaultAsync(
                item => item.LiveAgentSessionId == sessionId
                    && item.UserId == currentUser.UserId,
                cancellationToken);

        if (session is null)
        {
            return NotFound();
        }

        if (session.Status == LiveAgentSessionStatus.Resolved)
        {
            return Conflict("This Live Agent session has already been resolved.");
        }

        var supportFaqRecord = await _dbContext.SupportFaqRecords
            .FirstOrDefaultAsync(item => item.Id == session.SupportFaqId, cancellationToken);

        if (supportFaqRecord is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(session.FirstQuestion))
        {
            await AppendLiveAgentMessagesAsync(session, supportFaqRecord, currentUser, message, includeWaitingReply: true, cancellationToken);
        }

        return Ok(new CaptureLiveAgentQuestionResponse(
            session.LiveAgentSessionId,
            supportFaqRecord.Id,
            session.Status.ToString(),
            supportFaqRecord.Question,
            session.UpdatedAt));
    }

    [Authorize]
    [HttpPost("live-agent/sessions/{sessionId:int}/messages")]
    [EnableRateLimiting("help-ticket-create")]
    [ConditionalValidateAntiForgeryToken]
    public async Task<ActionResult<AppendLiveAgentMessageResponse>> AppendLiveAgentMessage(int sessionId, [FromBody] AppendLiveAgentMessageRequest request, CancellationToken cancellationToken)
    {
        var currentUser = await _authenticatedUserContextService.GetCurrentAsync(User, cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized();
        }

        var message = request.Message.Trim();
        if (string.IsNullOrWhiteSpace(message))
        {
            return BadRequest("Message is required.");
        }

        await NormalizeCurrentLiveAgentSessionAsync(currentUser.UserId, cancellationToken);

        var session = await _dbContext.LiveAgentSessions
            .FirstOrDefaultAsync(
                item => item.LiveAgentSessionId == sessionId
                    && item.UserId == currentUser.UserId,
                cancellationToken);

        if (session is null)
        {
            return NotFound();
        }

        if (session.Status == LiveAgentSessionStatus.Resolved)
        {
            return Conflict("This Live Agent session has already been resolved.");
        }

        var supportFaqRecord = await _dbContext.SupportFaqRecords
            .FirstOrDefaultAsync(item => item.Id == session.SupportFaqId, cancellationToken);

        if (supportFaqRecord is null)
        {
            return NotFound();
        }

        var messages = await AppendLiveAgentMessagesAsync(session, supportFaqRecord, currentUser, message, includeWaitingReply: true, cancellationToken);
        var assignedAgentName = await ResolveAssignedAgentNameAsync(supportFaqRecord, cancellationToken);

        return Ok(new AppendLiveAgentMessageResponse(
            session.LiveAgentSessionId,
            supportFaqRecord.Id,
            session.Status.ToString(),
            !string.IsNullOrWhiteSpace(session.FirstQuestion),
            HasAssignedAgent(supportFaqRecord),
            assignedAgentName,
            session.UpdatedAt,
            messages));
    }

    [Authorize]
    [HttpPost("live-agent/sessions/{sessionId:int}/resolve")]
    [EnableRateLimiting("help-ticket-create")]
    [ConditionalValidateAntiForgeryToken]
    public async Task<ActionResult<ResolveLiveAgentSessionResponse>> ResolveLiveAgentSession(int sessionId, CancellationToken cancellationToken)
    {
        var currentUser = await _authenticatedUserContextService.GetCurrentAsync(User, cancellationToken);
        if (currentUser is null)
        {
            return Unauthorized();
        }

        await NormalizeCurrentLiveAgentSessionAsync(currentUser.UserId, cancellationToken);

        var session = await _dbContext.LiveAgentSessions
            .FirstOrDefaultAsync(
                item => item.LiveAgentSessionId == sessionId
                    && item.UserId == currentUser.UserId,
                cancellationToken);

        if (session is null)
        {
            return NotFound();
        }

        var timestamp = DateTime.UtcNow;
        if (session.Status != LiveAgentSessionStatus.Resolved)
        {
            var supportFaqRecord = await _dbContext.SupportFaqRecords
                .FirstOrDefaultAsync(item => item.Id == session.SupportFaqId, cancellationToken);

            if (supportFaqRecord is null)
            {
                return NotFound();
            }

            ResolveLiveAgentSessionState(session, supportFaqRecord, LiveAgentSessionEndedReason.Resolved, timestamp);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Ok(new ResolveLiveAgentSessionResponse(
            session.LiveAgentSessionId,
            session.Status.ToString(),
            session.UpdatedAt));
    }

    private async Task<LiveAgentSession?> NormalizeCurrentLiveAgentSessionAsync(int userId, CancellationToken cancellationToken)
    {
        var unresolvedSessions = await _dbContext.LiveAgentSessions
            .Where(item => item.UserId == userId && item.Status != LiveAgentSessionStatus.Resolved)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.LiveAgentSessionId)
            .ToListAsync(cancellationToken);

        if (unresolvedSessions.Count == 0)
        {
            return null;
        }

        var timestamp = DateTime.UtcNow;
        var supportFaqIds = unresolvedSessions
            .Select(item => item.SupportFaqId)
            .Distinct()
            .ToArray();
        var supportFaqRecords = supportFaqIds.Length == 0
            ? new Dictionary<int, SupportFaqRecord>()
            : await _dbContext.SupportFaqRecords
                .Where(item => supportFaqIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, cancellationToken);
        var hasChanges = false;

        foreach (var inactiveSession in unresolvedSessions.Where(item => IsLiveAgentSessionInactive(item, timestamp)).ToList())
        {
            if (supportFaqRecords.TryGetValue(inactiveSession.SupportFaqId, out var supportFaqRecord))
            {
                ResolveLiveAgentSessionState(inactiveSession, supportFaqRecord, LiveAgentSessionEndedReason.Inactive, timestamp);
            }
            else
            {
                ResolveLiveAgentSessionWithoutSupportRecord(inactiveSession, LiveAgentSessionEndedReason.Inactive, timestamp);
            }

            hasChanges = true;
        }

        unresolvedSessions = unresolvedSessions
            .Where(item => item.Status != LiveAgentSessionStatus.Resolved)
            .ToList();

        if (unresolvedSessions.Count == 0)
        {
            if (hasChanges)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            return null;
        }

        if (unresolvedSessions.Count == 1)
        {
            if (hasChanges)
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            return unresolvedSessions[0];
        }

        var activeSession = unresolvedSessions[0];
        var duplicateSessions = unresolvedSessions.Skip(1).ToList();

        foreach (var duplicateSession in duplicateSessions)
        {
            if (supportFaqRecords.TryGetValue(duplicateSession.SupportFaqId, out var supportFaqRecord))
            {
                ResolveLiveAgentSessionState(duplicateSession, supportFaqRecord, LiveAgentSessionEndedReason.Resolved, timestamp);
            }
            else
            {
                ResolveLiveAgentSessionWithoutSupportRecord(duplicateSession, LiveAgentSessionEndedReason.Resolved, timestamp);
            }

            hasChanges = true;
        }

        if (hasChanges)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return activeSession;
    }

    private async Task<LastEndedLiveAgentNoticeResponse?> CreateLastEndedLiveAgentNoticeAsync(int userId, CancellationToken cancellationToken)
    {
        var session = await _dbContext.LiveAgentSessions
            .AsNoTracking()
            .Where(item => item.UserId == userId
                && item.Status == LiveAgentSessionStatus.Resolved
                && item.EndedReason != LiveAgentSessionEndedReason.None)
            .OrderByDescending(item => item.UpdatedAt)
            .ThenByDescending(item => item.LiveAgentSessionId)
            .FirstOrDefaultAsync(cancellationToken);

        if (session is null)
        {
            return null;
        }

        return new LastEndedLiveAgentNoticeResponse(
            session.LiveAgentSessionId,
            session.CategoryTitle,
            session.EndedReason.ToString(),
            session.UpdatedAt);
    }

    private async Task<LiveAgentSessionResponse> CreateLiveAgentSessionResponseAsync(LiveAgentSession session, CancellationToken cancellationToken)
    {
        var supportFaqRecord = await _dbContext.SupportFaqRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == session.SupportFaqId, cancellationToken);
        var hasAssignedAgent = HasAssignedAgent(supportFaqRecord);
        var assignedAgentName = await ResolveAssignedAgentNameAsync(supportFaqRecord, cancellationToken);
        var messages = await GetLiveAgentMessagesAsync(session.SupportFaqId, hasAssignedAgent, cancellationToken);

        return new LiveAgentSessionResponse(
            session.LiveAgentSessionId,
            session.SupportFaqId,
            session.Status.ToString(),
            session.CreatedAt,
            session.UpdatedAt,
            session.CategorySlug,
            session.CategoryTitle,
            !string.IsNullOrWhiteSpace(session.FirstQuestion),
            hasAssignedAgent,
            assignedAgentName,
            messages);
    }

    private async Task<string?> ResolveAssignedAgentNameAsync(SupportFaqRecord? supportFaqRecord, CancellationToken cancellationToken)
    {
        if (supportFaqRecord?.AgentId is not int agentUserId)
        {
            return null;
        }

        return await _dbContext.SupportAgents
            .AsNoTracking()
            .Where(item => item.UserId == agentUserId)
            .OrderByDescending(item => item.AgentStatus == "available")
            .ThenByDescending(item => item.ChatId)
            .Select(item => item.AgentName)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<LiveAgentMessageDto>> GetLiveAgentMessagesAsync(
        int supportFaqId,
        bool suppressQueueMessages,
        CancellationToken cancellationToken)
    {
        var messages = await _dbContext.SupportMessages
            .AsNoTracking()
            .Where(item => item.ConversationId == supportFaqId)
            .OrderBy(item => item.CreatedAt)
            .ThenBy(item => item.Id)
            .Select(item => new LiveAgentMessageDto(
                item.Id,
                item.ConversationId,
                item.SenderId,
                item.SenderRole,
                item.MessageText,
                item.CreatedAt))
            .ToListAsync(cancellationToken);

        if (!suppressQueueMessages)
        {
            return messages;
        }

        return messages
            .Where(item => !IsQueuedSystemMessage(item))
            .ToList();
    }

    private async Task<IReadOnlyList<LiveAgentMessageDto>> AppendLiveAgentMessagesAsync(
        LiveAgentSession session,
        SupportFaqRecord supportFaqRecord,
        AuthenticatedUserContext currentUser,
        string message,
        bool includeWaitingReply,
        CancellationToken cancellationToken)
    {
        var timestamp = DateTime.UtcNow;
        var createdMessages = new List<SupportMessage>();

        var userMessage = new SupportMessage
        {
            ConversationId = session.SupportFaqId,
            SenderId = currentUser.ConsumerId ?? currentUser.UserId,
            SenderRole = ConsumerSenderRole,
            MessageText = message,
            CreatedAt = timestamp,
        };

        createdMessages.Add(userMessage);
        _dbContext.SupportMessages.Add(userMessage);

        if (string.IsNullOrWhiteSpace(session.FirstQuestion))
        {
            session.FirstQuestion = message;
        }

        if (string.IsNullOrWhiteSpace(supportFaqRecord.Question))
        {
            supportFaqRecord.Question = message;
        }

        session.UpdatedAt = timestamp;

        if (includeWaitingReply && !HasAssignedAgent(supportFaqRecord))
        {
            var agentMessage = new SupportMessage
            {
                ConversationId = session.SupportFaqId,
                SenderId = 0,
                SenderRole = SupportAgentRole,
                MessageText = LiveAgentWaitingReplyMessage,
                CreatedAt = timestamp,
            };

            createdMessages.Add(agentMessage);
            _dbContext.SupportMessages.Add(agentMessage);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return createdMessages
            .Select(item => new LiveAgentMessageDto(
                item.Id,
                item.ConversationId,
                item.SenderId,
                item.SenderRole,
                item.MessageText,
                item.CreatedAt))
            .ToList();
    }

    private static bool HasAssignedAgent(SupportFaqRecord? supportFaqRecord)
        => supportFaqRecord?.AgentId is not null;

    private static bool IsLiveAgentSessionInactive(LiveAgentSession session, DateTime timestamp)
        => timestamp - session.UpdatedAt >= LiveAgentInactivityTimeout;

    private static void ResolveLiveAgentSessionState(
        LiveAgentSession session,
        SupportFaqRecord supportFaqRecord,
        LiveAgentSessionEndedReason endedReason,
        DateTime timestamp)
    {
        supportFaqRecord.Status = "Resolved";
        supportFaqRecord.EndTime = timestamp;
        supportFaqRecord.DurationMinutes = supportFaqRecord.StartTime.HasValue
            ? Math.Max(0, (int)(timestamp - supportFaqRecord.StartTime.Value).TotalMinutes)
            : 0;

        session.Status = LiveAgentSessionStatus.Resolved;
        session.EndedReason = endedReason;
        session.UpdatedAt = timestamp;
    }

    private static void ResolveLiveAgentSessionWithoutSupportRecord(
        LiveAgentSession session,
        LiveAgentSessionEndedReason endedReason,
        DateTime timestamp)
    {
        session.Status = LiveAgentSessionStatus.Resolved;
        session.EndedReason = endedReason;
        session.UpdatedAt = timestamp;
    }

    private static bool IsQueuedSystemMessage(LiveAgentMessageDto item)
        => string.Equals(item.SenderRole, SupportAgentRole, StringComparison.Ordinal)
            && (string.Equals(item.MessageText, LiveAgentWaitingReplyMessage, StringComparison.Ordinal)
                || string.Equals(item.MessageText, LiveAgentQueueWelcomeMessage, StringComparison.Ordinal));

    private IQueryable<FaqRecord> QueryVisibleFaqs()
        => _dbContext.FaqRecords
            .AsNoTracking()
            .Where(faq => faq.UserType == ConsumerUserType
                && faq.Status == ActiveStatus
                && faq.Category != null
                && faq.Category.Trim() != string.Empty);

    private async Task<IReadOnlyList<HelpCategorySummaryDto>> GetCategorySummariesAsync(CancellationToken cancellationToken)
    {
        var categoryNames = await GetVisibleCategoryNamesAsync(cancellationToken);
        return categoryNames
            .Select((categoryName, index) => CreateCategorySummary(categoryName, index + 1))
            .ToList();
    }

    private async Task<IReadOnlyList<string>> GetVisibleCategoryNamesAsync(CancellationToken cancellationToken)
    {
        var categoryNames = await QueryVisibleFaqs()
            .Select(faq => faq.Category)
            .OrderBy(category => category)
            .ToListAsync(cancellationToken);

        var results = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var categoryName in categoryNames)
        {
            var trimmed = categoryName?.Trim();
            var normalized = NormalizeSlug(trimmed);
            if (string.IsNullOrWhiteSpace(trimmed) || !seen.Add(normalized))
            {
                continue;
            }

            results.Add(trimmed);
        }

        return results;
    }

    private async Task<string?> ResolveCategoryNameAsync(string normalizedSlug, CancellationToken cancellationToken)
    {
        var categories = await GetVisibleCategoryNamesAsync(cancellationToken);
        return categories.FirstOrDefault(category => NormalizeSlug(category) == normalizedSlug);
    }

    private async Task<int> GetCategoryDisplayOrderAsync(string categoryName, CancellationToken cancellationToken)
    {
        var categories = await GetVisibleCategoryNamesAsync(cancellationToken);
        for (var index = 0; index < categories.Count; index++)
        {
            if (string.Equals(categories[index], categoryName, StringComparison.Ordinal))
            {
                return index + 1;
            }
        }

        return int.MaxValue;
    }

    private static HelpCategorySummaryDto CreateCategorySummary(string categoryName, int displayOrder)
        => new(
            NormalizeSlug(categoryName),
            categoryName,
            $"Consumer FAQs for {categoryName}.",
            DefaultCategoryIcon,
            displayOrder);

    private static string NormalizeSlug(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        var pendingSeparator = false;

        foreach (var character in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                if (pendingSeparator && builder.Length > 0)
                {
                    builder.Append('-');
                }

                builder.Append(character);
                pendingSeparator = false;
                continue;
            }

            if (character == '&')
            {
                if (pendingSeparator && builder.Length > 0)
                {
                    builder.Append('-');
                }

                builder.Append("and");
                pendingSeparator = false;
                continue;
            }

            pendingSeparator = builder.Length > 0;
        }

        return builder.ToString().Trim('-');
    }

    private static string CreateReferenceCode()
    {
        Span<byte> bytes = stackalloc byte[4];
        RandomNumberGenerator.Fill(bytes);
        var suffix = Convert.ToHexString(bytes);
        return $"HC-{DateTime.UtcNow:yyyyMMddHHmmss}-{suffix}";
    }
}

