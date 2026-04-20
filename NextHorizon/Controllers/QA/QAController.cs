using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NextHorizon.Models.QA;
using NextHorizon.Services;

namespace NextHorizon.Controllers;

[Route("qa")]
public sealed class QAController : Controller
{
    private const string QaAnalystRole = "QA Analyst";
    private const string QaHeadRole = "QA Head";
    private const string SupportAgentRole = "Support Agent";

    private readonly IQaAgentTicketsService _qaAgentTicketsService;
    private readonly IQaAgentsService _qaAgentsService;
    private readonly IQaDashboardService _qaDashboardService;
    private readonly IQaEvaluationService _qaEvaluationService;
    private readonly INotificationService _notificationService;
    private readonly IQaRatingQueueService _qaRatingQueueService;
    private readonly IQaRatedHistoryService _qaRatedHistoryService;
    private readonly IQaResolvedTicketsService _qaResolvedTicketsService;
    private readonly IQaReviewService _qaReviewService;

    public QAController(
        IQaAgentTicketsService qaAgentTicketsService,
        IQaAgentsService qaAgentsService,
        IQaDashboardService qaDashboardService,
        IQaEvaluationService qaEvaluationService,
        INotificationService notificationService,
        IQaRatingQueueService qaRatingQueueService,
        IQaRatedHistoryService qaRatedHistoryService,
        IQaResolvedTicketsService qaResolvedTicketsService,
        IQaReviewService qaReviewService)
    {
        _qaAgentTicketsService = qaAgentTicketsService;
        _qaAgentsService = qaAgentsService;
        _qaDashboardService = qaDashboardService;
        _qaEvaluationService = qaEvaluationService;
        _notificationService = notificationService;
        _qaRatingQueueService = qaRatingQueueService;
        _qaRatedHistoryService = qaRatedHistoryService;
        _qaResolvedTicketsService = qaResolvedTicketsService;
        _qaReviewService = qaReviewService;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (IsAuthorizedQaSession())
        {
            base.OnActionExecuting(context);
            return;
        }

        var userType = HttpContext.Session.GetString("UserType");
        var isSupportAgent = IsSupportAgentSession(userType);

        if (Request.Path.StartsWithSegments("/qa/api", StringComparison.OrdinalIgnoreCase))
        {
            context.Result = Unauthorized();
            return;
        }

        if (isSupportAgent)
        {
            context.Result = RedirectToAction("AgentDashboard", "Agent");
            return;
        }

        if (HttpContext.Session.GetInt32("StaffId").HasValue)
        {
            HttpContext.Session.Clear();
            TempData["LoginError"] = "QA workspace access is restricted to QA Analyst and QA Head users only.";
        }

        context.Result = RedirectToAction("AdminLogin", "Login");
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        return RedirectToAction(nameof(Dashboard));
    }

    [HttpGet("dashboard")]
    public IActionResult Dashboard()
    {
        ViewData["Title"] = "QA Dashboard";
        return View();
    }

    [HttpGet("api/dashboard")]
    public async Task<IActionResult> DashboardData([FromQuery] DateOnly? from, [FromQuery] DateOnly? to, CancellationToken cancellationToken)
    {
        if (!from.HasValue || !to.HasValue)
        {
            return BadRequest(new { message = "Both from and to dates are required." });
        }

        if (from.Value > to.Value)
        {
            return BadRequest(new { message = "The from date must be earlier than or equal to the to date." });
        }

        var payload = await _qaDashboardService.GetDashboardAsync(from.Value, to.Value, cancellationToken);
        return Json(payload);
    }

    [HttpGet("rating/{id:int?}")]
    public async Task<IActionResult> Rating(
        int id = 0,
        [FromQuery] string? range = null,
        [FromQuery] DateOnly? from = null,
        [FromQuery] DateOnly? to = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "QA Rating Page";

        if (id <= 0)
        {
            return await RedirectToQueueOrRenderEmptyAsync(range, from, to, search, cancellationToken);
        }

        var reviewerDisplayName = GetReviewerDisplayName();
        var pageData = await _qaReviewService.GetRatingPageAsync(
            id,
            reviewerDisplayName,
            range,
            from,
            to,
            search,
            cancellationToken);
        if (pageData is not null)
        {
            ViewData["Title"] = "QA Rating Page";
            return View(pageData);
        }

        return await RedirectToQueueOrRenderEmptyAsync(range, from, to, search, cancellationToken);
    }

    [HttpPost("api/reviews/{supportFaqId:int}")]
    public async Task<IActionResult> UpsertReview(int supportFaqId, [FromBody] QaReviewUpsertRequest? request, CancellationToken cancellationToken)
    {
        if (!TryGetReviewerContext(out var reviewerStaffId, out var reviewerName))
        {
            return Unauthorized();
        }

        if (request is null)
        {
            return BadRequest(new QaReviewMutationResponse(false, "Invalid QA review payload."));
        }

        var response = await _qaReviewService.UpsertReviewAsync(
            supportFaqId,
            reviewerStaffId,
            reviewerName,
            request,
            cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Json(response);
    }

    [HttpPost("api/reviews/{supportFaqId:int}/submit-agent")]
    public async Task<IActionResult> SubmitReviewToAgent(int supportFaqId, CancellationToken cancellationToken)
    {
        if (!TryGetReviewerContext(out var reviewerStaffId, out var reviewerName))
        {
            return Unauthorized();
        }

        var response = await _qaReviewService.SubmitToAgentAsync(
            supportFaqId,
            reviewerStaffId,
            reviewerName,
            cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Json(response);
    }

    [HttpPost("api/reviews/{supportFaqId:int}/inline-comments")]
    public async Task<IActionResult> UpsertInlineComments(
        int supportFaqId,
        [FromBody] QaInlineCommentsUpsertRequest? request,
        CancellationToken cancellationToken)
    {
        if (!TryGetReviewerContext(out var reviewerStaffId, out var reviewerName))
        {
            return Unauthorized();
        }

        if (request is null)
        {
            return BadRequest(new QaReviewMutationResponse(false, "Invalid QA inline comments payload."));
        }

        var response = await _qaReviewService.UpsertInlineCommentsAsync(
            supportFaqId,
            reviewerStaffId,
            reviewerName,
            request,
            cancellationToken);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Json(response);
    }

    [HttpGet("agents")]
    public async Task<IActionResult> AllAgents(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Agents";
        var agents = await _qaAgentsService.GetAgentsAsync(cancellationToken);
        return View(agents);
    }

    [HttpGet("resolved-tickets")]
    public IActionResult AllResolvedTickets()
    {
        ViewData["Title"] = "Resolved Tickets Awaiting QA";
        return View();
    }

    [HttpGet("api/rating-queue")]
    public async Task<IActionResult> RatingQueueData(
        [FromQuery] string? range,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? search,
        [FromQuery] int? ticketId,
        CancellationToken cancellationToken)
    {
        var payload = await _qaRatingQueueService.GetQueueAsync(
            range,
            from,
            to,
            search,
            ticketId,
            cancellationToken);

        return Json(payload);
    }

    [HttpGet("api/agent-tickets")]
    public async Task<IActionResult> AgentTicketsData(
        [FromQuery] int id,
        [FromQuery] int? awaitingPage,
        [FromQuery] int? ratedPage,
        [FromQuery] string? range,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? search,
        [FromQuery] string? score,
        CancellationToken cancellationToken)
    {
        var payload = await _qaAgentTicketsService.GetAgentTicketsAsync(
            id,
            awaitingPage ?? 1,
            ratedPage ?? 1,
            range,
            from,
            to,
            search,
            score,
            cancellationToken);

        if (payload is null)
        {
            return NotFound();
        }

        return Json(payload);
    }

    [HttpGet("api/resolved-tickets")]
    public async Task<IActionResult> ResolvedTicketsData(
        [FromQuery] int? page,
        [FromQuery] string? range,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var payload = await _qaResolvedTicketsService.GetResolvedTicketsAsync(
            page ?? 1,
            range,
            from,
            to,
            search,
            cancellationToken);

        return Json(payload);
    }

    [HttpGet("api/rated-history")]
    public async Task<IActionResult> RatedHistoryData(
        [FromQuery] int? page,
        [FromQuery] string? range,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? search,
        [FromQuery] string? score,
        CancellationToken cancellationToken)
    {
        var payload = await _qaRatedHistoryService.GetRatedHistoryAsync(
            page ?? 1,
            range,
            from,
            to,
            search,
            score,
            cancellationToken);

        return Json(payload);
    }

    [HttpGet("agent-tickets")]
    public IActionResult AgentTickets(int id = 0, string? agentName = null)
    {
        ViewData["Title"] = "Agent Tickets";
        ViewData["AgentId"] = id;
        ViewData["AgentName"] = (agentName ?? string.Empty).Trim();
        return View();
    }

    [HttpGet("rated-history")]
    public IActionResult RatedHistory()
    {
        ViewData["Title"] = "Rated History";
        return View();
    }

    [HttpGet("evaluations")]
    public async Task<IActionResult> Evaluations(CancellationToken cancellationToken)
    {
        if (!IsQaHeadSession())
        {
            return RedirectToAction(nameof(Dashboard));
        }

        ViewData["Title"] = "QA Evaluations";
        var pageData = await _qaEvaluationService.GetPageDataAsync(cancellationToken);
        return View(pageData);
    }

    [HttpPost("api/evaluations")]
    public async Task<IActionResult> SaveEvaluations(
        [FromBody] QaEvaluationTemplateUpsertRequest? request,
        CancellationToken cancellationToken)
    {
        if (!IsQaHeadSession())
        {
            return Forbid();
        }

        if (request is null)
        {
            return BadRequest(new QaEvaluationTemplateMutationResponse(false, "Invalid QA evaluation payload."));
        }

        var updatedById = HttpContext.Session.GetInt32("StaffId");
        if (!updatedById.HasValue || updatedById.Value <= 0)
        {
            return Unauthorized();
        }

        var response = await _qaEvaluationService.SaveTemplateAsync(
            request,
            updatedById.Value,
            GetReviewerDisplayName(),
            cancellationToken);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Json(response);
    }

    [HttpPost("api/evaluations/{templateId:int}/reuse")]
    public async Task<IActionResult> TrackEvaluationReuse(int templateId, CancellationToken cancellationToken)
    {
        if (!IsQaHeadSession())
        {
            return Forbid();
        }

        var response = await _qaEvaluationService.TrackReuseAsync(
            templateId,
            GetReviewerDisplayName(),
            cancellationToken);
        if (!response.Success)
        {
            return BadRequest(response);
        }

        return Json(response);
    }

    [HttpGet("api/evaluations/{templateId:int}")]
    public async Task<IActionResult> EvaluationTemplateDetail(int templateId, CancellationToken cancellationToken)
    {
        if (!IsAuthorizedQaSession())
        {
            return Unauthorized();
        }

        var template = await _qaEvaluationService.GetTemplateAsync(templateId, cancellationToken);
        if (template is null)
        {
            return NotFound();
        }

        return Json(template);
    }

    [HttpPost("api/notifications/read")]
    public async Task<IActionResult> MarkNotificationsRead(CancellationToken cancellationToken)
    {
        if (!IsAuthorizedQaSession())
        {
            return Unauthorized();
        }

        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue || userId.Value <= 0)
        {
            return Unauthorized();
        }

        await _notificationService.MarkAllReadAsync(userId.Value, cancellationToken);
        return Json(new { success = true });
    }

    [HttpPost("api/notifications/clear")]
    public async Task<IActionResult> ClearNotifications(CancellationToken cancellationToken)
    {
        if (!IsAuthorizedQaSession())
        {
            return Unauthorized();
        }

        var userId = HttpContext.Session.GetInt32("UserId");
        if (!userId.HasValue || userId.Value <= 0)
        {
            return Unauthorized();
        }

        await _notificationService.ClearAllAsync(userId.Value, cancellationToken);
        return Json(new { success = true });
    }

    private string GetReviewerDisplayName()
    {
        return HttpContext.Session.GetString("FullName")
            ?? HttpContext.Session.GetString("Username")
            ?? "QA Reviewer - You";
    }

    private bool TryGetReviewerContext(out int reviewerStaffId, out string reviewerName)
    {
        reviewerStaffId = HttpContext.Session.GetInt32("StaffId") ?? 0;
        reviewerName = GetReviewerDisplayName();
        return reviewerStaffId > 0 && IsAuthorizedQaSession();
    }

    private bool IsAuthorizedQaSession()
    {
        var staffId = HttpContext.Session.GetInt32("StaffId");
        var userType = HttpContext.Session.GetString("UserType");

        return staffId.HasValue
            && staffId.Value > 0
            && IsQaWorkspaceRole(userType);
    }

    private bool IsQaHeadSession()
    {
        var staffId = HttpContext.Session.GetInt32("StaffId");
        var userType = HttpContext.Session.GetString("UserType");

        return staffId.HasValue
            && staffId.Value > 0
            && string.Equals(userType?.Trim(), QaHeadRole, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSupportAgentSession(string? userType)
    {
        return string.Equals(userType?.Trim(), SupportAgentRole, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsQaWorkspaceRole(string? userType)
    {
        var normalizedUserType = userType?.Trim() ?? string.Empty;

        return string.Equals(normalizedUserType, QaAnalystRole, StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedUserType, QaHeadRole, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<IActionResult> RedirectToQueueOrRenderEmptyAsync(
        string? range,
        DateOnly? from,
        DateOnly? to,
        string? search,
        CancellationToken cancellationToken)
    {
        var queueState = await _qaRatingQueueService.GetQueueAsync(
            range,
            from,
            to,
            search,
            null,
            cancellationToken);

        var targetSupportFaqId = queueState.Items.FirstOrDefault()?.SupportFaqId;
        if (targetSupportFaqId.HasValue)
        {
            return RedirectToAction(nameof(Rating), new
            {
                id = targetSupportFaqId.Value,
                range,
                from,
                to,
                search
            });
        }

        var activeTemplate = await _qaEvaluationService.GetActiveTemplateAsync(cancellationToken);
        return View("Rating", new QaRatingPageData
        {
            SupportFaqId = 0,
            HasActiveTicket = false,
            PreviousSupportFaqId = 0,
            NextSupportFaqId = 0,
            AgentName = string.Empty,
            CustomerName = string.Empty,
            ConversationDateLabel = string.Empty,
            Messages = Array.Empty<QaConversationMessageViewModel>(),
            ReviewerDisplayName = GetReviewerDisplayName(),
            InlineCommentsJson = "{}",
            Review = null,
            EvaluationTemplate = activeTemplate,
            QueueCount = queueState.QueueCount,
            QueuePosition = null,
            CurrentTicketInQueue = false,
            QueueRange = NormalizeRange(range),
            QueueFrom = from,
            QueueTo = to,
            QueueSearch = (search ?? string.Empty).Trim()
        });
    }

    private static string NormalizeRange(string? range)
    {
        var normalized = (range ?? "custom").Trim().ToLowerInvariant();
        return normalized is "today" or "last7" or "last30" ? normalized : "custom";
    }
}
