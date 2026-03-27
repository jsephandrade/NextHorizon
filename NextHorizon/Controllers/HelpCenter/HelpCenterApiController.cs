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
            HelpCategoryId = null,
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

        var timestamp = DateTime.UtcNow;
        var supportFaqRecord = new SupportFaqRecord
        {
            Category = categoryName,
            Question = string.Empty,
            Resolution = "Waiting",
            DurationMinutes = 0,
            UserType = ConsumerUserType,
            AgentId = null,
            CreatedAt = timestamp,
        };

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        _dbContext.SupportFaqRecords.Add(supportFaqRecord);
        await _dbContext.SaveChangesAsync(cancellationToken);

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
            new LiveAgentSessionResponse(
                session.LiveAgentSessionId,
                supportFaqRecord.Id,
                session.Status.ToString(),
                session.CreatedAt,
                session.CategorySlug,
                session.CategoryTitle));
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

        var timestamp = DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(session.FirstQuestion))
        {
            session.FirstQuestion = message;
            session.UpdatedAt = timestamp;
        }

        if (string.IsNullOrWhiteSpace(supportFaqRecord.Question))
        {
            supportFaqRecord.Question = message;
        }

        if (session.Status == LiveAgentSessionStatus.Waiting)
        {
            session.Status = LiveAgentSessionStatus.Active;
            session.UpdatedAt = timestamp;
        }

        if (string.Equals(supportFaqRecord.Resolution, "Waiting", StringComparison.Ordinal))
        {
            supportFaqRecord.Resolution = "Active";
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(new CaptureLiveAgentQuestionResponse(
            session.LiveAgentSessionId,
            supportFaqRecord.Id,
            session.Status.ToString(),
            supportFaqRecord.Question,
            session.UpdatedAt));
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

            supportFaqRecord.Resolution = "Resolved";
            session.Status = LiveAgentSessionStatus.Resolved;
            session.UpdatedAt = timestamp;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Ok(new ResolveLiveAgentSessionResponse(
            session.LiveAgentSessionId,
            session.Status.ToString(),
            session.UpdatedAt));
    }

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

