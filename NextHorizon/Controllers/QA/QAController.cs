using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NextHorizon.Models;
using NextHorizon.Models.QA;
using NextHorizon.Services;

namespace NextHorizon.Controllers;

[Route("qa")]
public sealed class QAController : Controller
{
    private const string QaAnalystRole = "QA Analyst";
    private const string SupportAgentRole = "Support Agent";

    private readonly IQaAgentTicketsService _qaAgentTicketsService;
    private readonly IQaAgentsService _qaAgentsService;
    private readonly IQaDashboardService _qaDashboardService;
    private readonly IQaRatingQueueService _qaRatingQueueService;
    private readonly IQaRatedHistoryService _qaRatedHistoryService;
    private readonly IQaResolvedTicketsService _qaResolvedTicketsService;
    private readonly IQaReviewService _qaReviewService;

    public QAController(
        IQaAgentTicketsService qaAgentTicketsService,
        IQaAgentsService qaAgentsService,
        IQaDashboardService qaDashboardService,
        IQaRatingQueueService qaRatingQueueService,
        IQaRatedHistoryService qaRatedHistoryService,
        IQaResolvedTicketsService qaResolvedTicketsService,
        IQaReviewService qaReviewService)
    {
        _qaAgentTicketsService = qaAgentTicketsService;
        _qaAgentsService = qaAgentsService;
        _qaDashboardService = qaDashboardService;
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
            TempData["LoginError"] = "QA workspace access is restricted to QA Analyst users only.";
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
    public async Task<IActionResult> DashboardData([FromQuery] DateOnly? date, CancellationToken cancellationToken)
    {
        var selectedDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var payload = await _qaDashboardService.GetDashboardAsync(selectedDate, cancellationToken);
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
        ApplyQueueViewState(range, from, to, search);

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
            ApplyRatingPageData(pageData);
            return View();
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

    private void ApplyRatingPageData(QaRatingPageData pageData)
    {
        ViewData["HasActiveTicket"] = pageData.HasActiveTicket;
        ViewData["ConversationId"] = pageData.SupportFaqId;
        ViewData["ConversationAgent"] = pageData.AgentName;
        ViewData["ConversationCustomer"] = pageData.CustomerName;
        ViewData["ConversationDate"] = pageData.ConversationDateLabel;
        ViewData["ConversationMessages"] = pageData.Messages;
        ViewData["ReviewerDisplayName"] = pageData.ReviewerDisplayName;
        ViewData["InlineCommentsJson"] = pageData.InlineCommentsJson;
        ViewData["PrevTicketId"] = pageData.PreviousSupportFaqId ?? pageData.SupportFaqId;
        ViewData["NextTicketId"] = pageData.NextSupportFaqId ?? pageData.SupportFaqId;
        ViewData["QueueCount"] = pageData.QueueCount;
        ViewData["QueuePosition"] = pageData.QueuePosition;
        ViewData["CurrentTicketInQueue"] = pageData.CurrentTicketInQueue;
        ApplyQueueViewState(pageData.QueueRange, pageData.QueueFrom, pageData.QueueTo, pageData.QueueSearch);

        if (pageData.Review is null)
        {
            ViewData["IsRated"] = false;
            return;
        }

        var scoreMap = pageData.Review.QuestionScores
            .ToDictionary(item => item.QuestionKey, item => item.Score, StringComparer.OrdinalIgnoreCase);

        var ratedAccuracy = ResolveAverage(scoreMap, "accuracy_q1", "accuracy_q2", "accuracy_q3");
        var ratedTone = ResolveAverage(scoreMap, "tone_q1", "tone_q2", "tone_q3");
        var ratedResolution = ResolveAverage(scoreMap, "resolution_q1", "resolution_q2", "resolution_q3");

        ViewData["IsRated"] = true;
        ViewData["RatedAccuracy"] = ratedAccuracy;
        ViewData["RatedTone"] = ratedTone;
        ViewData["RatedResolution"] = ratedResolution;
        ViewData["RatedAccuracyQ1"] = scoreMap.GetValueOrDefault("accuracy_q1", ratedAccuracy);
        ViewData["RatedAccuracyQ2"] = scoreMap.GetValueOrDefault("accuracy_q2", ratedAccuracy);
        ViewData["RatedAccuracyQ3"] = scoreMap.GetValueOrDefault("accuracy_q3", ratedAccuracy);
        ViewData["RatedToneQ1"] = scoreMap.GetValueOrDefault("tone_q1", ratedTone);
        ViewData["RatedToneQ2"] = scoreMap.GetValueOrDefault("tone_q2", ratedTone);
        ViewData["RatedToneQ3"] = scoreMap.GetValueOrDefault("tone_q3", ratedTone);
        ViewData["RatedResolutionQ1"] = scoreMap.GetValueOrDefault("resolution_q1", ratedResolution);
        ViewData["RatedResolutionQ2"] = scoreMap.GetValueOrDefault("resolution_q2", ratedResolution);
        ViewData["RatedResolutionQ3"] = scoreMap.GetValueOrDefault("resolution_q3", ratedResolution);
        ViewData["RatedNotes"] = pageData.Review.Notes;
        ViewData["RatedBy"] = pageData.Review.ReviewerName;
        ViewData["RatedOn"] = pageData.Review.CreatedAtUtc.ToString("MMM dd, yyyy hh:mm tt");
        ViewData["RatedSubmittedToAgent"] = pageData.Review.SubmittedToAgent;
        ViewData["RatedSubmittedOn"] = pageData.Review.SubmittedToAgentAtUtc?.ToString("MMM dd, yyyy hh:mm tt") ?? string.Empty;
        ViewData["RatedAccuracyPoints"] = (double)Math.Round((pageData.Review.AccuracyAverage / 5m) * 35m, 1);
        ViewData["RatedTonePoints"] = (double)Math.Round((pageData.Review.ToneAverage / 5m) * 35m, 1);
        ViewData["RatedResolutionPoints"] = (double)Math.Round((pageData.Review.ResolutionAverage / 5m) * 30m, 1);
        ViewData["RatedOverallPercent"] = (double)Math.Round(pageData.Review.OverallPercent, 1);
    }

    private string GetReviewerDisplayName()
    {
        return HttpContext.Session.GetString("FullName")
            ?? HttpContext.Session.GetString("Username")
            ?? "QA Analyst - You";
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
            && string.Equals(userType?.Trim(), QaAnalystRole, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSupportAgentSession(string? userType)
    {
        return string.Equals(userType?.Trim(), SupportAgentRole, StringComparison.OrdinalIgnoreCase);
    }

    private static int ResolveAverage(IReadOnlyDictionary<string, int> scoreMap, params string[] keys)
    {
        var values = keys
            .Select(key => scoreMap.TryGetValue(key, out var value) ? value : 0)
            .Where(value => value > 0)
            .ToArray();

        return values.Length == 0 ? 0 : (int)Math.Round(values.Average());
    }

    private void ApplyQueueViewState(string? range, DateOnly? from, DateOnly? to, string? search)
    {
        ViewData["QueueRange"] = NormalizeRange(range);
        ViewData["QueueFromDate"] = from?.ToString("yyyy-MM-dd") ?? string.Empty;
        ViewData["QueueToDate"] = to?.ToString("yyyy-MM-dd") ?? string.Empty;
        ViewData["QueueSearch"] = (search ?? string.Empty).Trim();
    }

    private static string NormalizeRange(string? range)
    {
        var normalized = (range ?? "custom").Trim().ToLowerInvariant();
        return normalized is "today" or "last7" or "last30" ? normalized : "custom";
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

        ApplyEmptyRatingPageData(range, from, to, search, queueState.QueueCount);
        return View("Rating");
    }

    private void ApplyEmptyRatingPageData(string? range, DateOnly? from, DateOnly? to, string? search, int queueCount)
    {
        ViewData["HasActiveTicket"] = false;
        ViewData["ConversationId"] = 0;
        ViewData["ConversationAgent"] = string.Empty;
        ViewData["ConversationCustomer"] = string.Empty;
        ViewData["ConversationDate"] = string.Empty;
        ViewData["ConversationMessages"] = Array.Empty<QaConversationMessageViewModel>();
        ViewData["ReviewerDisplayName"] = GetReviewerDisplayName();
        ViewData["InlineCommentsJson"] = "{}";
        ViewData["PrevTicketId"] = 0;
        ViewData["NextTicketId"] = 0;
        ViewData["QueueCount"] = queueCount;
        ViewData["QueuePosition"] = null;
        ViewData["CurrentTicketInQueue"] = false;
        ViewData["IsRated"] = false;
        ApplyQueueViewState(range, from, to, search);
    }
}
