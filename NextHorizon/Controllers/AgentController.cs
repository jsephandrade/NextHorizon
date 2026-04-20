using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NextHorizon.Models.Agent;
using NextHorizon.Services;

namespace NextHorizon.Controllers;

public sealed class AgentController : Controller
{
    private const string QaAnalystRole = "QA Analyst";
    private const string QaHeadRole = "QA Head";
    private const string SupportAgentRole = "Support Agent";
    private readonly IAgentDashboardService _agentDashboardService;

    public AgentController(IAgentDashboardService agentDashboardService)
    {
        _agentDashboardService = agentDashboardService;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var staffId = HttpContext.Session.GetInt32("StaffId");
        var userType = HttpContext.Session.GetString("UserType")?.Trim() ?? string.Empty;
        var isApiRequest = Request.Path.StartsWithSegments("/Agent/api", StringComparison.OrdinalIgnoreCase);

        if (!staffId.HasValue || staffId.Value <= 0)
        {
            context.Result = isApiRequest ? Unauthorized() : RedirectToAction("AdminLogin", "Login");
            return;
        }

        if (string.Equals(userType, SupportAgentRole, StringComparison.OrdinalIgnoreCase))
        {
            base.OnActionExecuting(context);
            return;
        }

        if (IsQaWorkspaceRole(userType))
        {
            context.Result = isApiRequest ? Unauthorized() : RedirectToAction("Dashboard", "QA");
            return;
        }

        HttpContext.Session.Clear();
        TempData["LoginError"] = "Agent workspace access is restricted to Support Agent users only.";
        context.Result = isApiRequest ? Unauthorized() : RedirectToAction("AdminLogin", "Login");
    }

    private static bool IsQaWorkspaceRole(string? userType)
    {
        var normalizedUserType = userType?.Trim() ?? string.Empty;

        return string.Equals(normalizedUserType, QaAnalystRole, StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedUserType, QaHeadRole, StringComparison.OrdinalIgnoreCase);
    }

    public IActionResult AgentDashboard()
    {
        ViewData["Title"] = "Agent Dashboard";
        return View();
    }

    public IActionResult Tickets()
    {
        ViewData["Title"] = "Agent Tickets";
        return View();
    }

    [HttpGet("/Agent/api/dashboard")]
    public async Task<IActionResult> DashboardData(CancellationToken cancellationToken)
    {
        var agentUserId = HttpContext.Session.GetInt32("UserId");
        if (!agentUserId.HasValue || agentUserId.Value <= 0)
        {
            return Unauthorized();
        }

        var payload = await _agentDashboardService.GetDashboardAsync(agentUserId.Value, cancellationToken);
        return Json(payload);
    }

    [HttpGet("/Agent/api/dashboard/{supportFaqId:int}")]
    public async Task<IActionResult> DashboardTicketDetail(int supportFaqId, CancellationToken cancellationToken)
    {
        var agentUserId = HttpContext.Session.GetInt32("UserId");
        if (!agentUserId.HasValue || agentUserId.Value <= 0)
        {
            return Unauthorized();
        }

        var payload = await _agentDashboardService.GetTicketDetailAsync(agentUserId.Value, supportFaqId, cancellationToken);
        if (payload is null)
        {
            return NotFound();
        }

        return Json(payload);
    }

    [HttpPost("/Agent/api/dashboard/{supportFaqId:int}/notes")]
    public async Task<IActionResult> SaveTicketNotes(int supportFaqId, [FromBody] AgentDashboardNotesSaveRequest? request, CancellationToken cancellationToken)
    {
        var agentUserId = HttpContext.Session.GetInt32("UserId");
        if (!agentUserId.HasValue || agentUserId.Value <= 0)
        {
            return Unauthorized();
        }

        var response = await _agentDashboardService.SaveNotesAsync(agentUserId.Value, supportFaqId, request?.Notes, cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("/Agent/api/dashboard/{supportFaqId:int}/acknowledge")]
    public async Task<IActionResult> AcknowledgeTicketReview(int supportFaqId, CancellationToken cancellationToken)
    {
        var agentUserId = HttpContext.Session.GetInt32("UserId");
        if (!agentUserId.HasValue || agentUserId.Value <= 0)
        {
            return Unauthorized();
        }

        var response = await _agentDashboardService.AcknowledgeAsync(agentUserId.Value, supportFaqId, cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }
}
