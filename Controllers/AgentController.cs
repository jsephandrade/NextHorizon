using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NextHorizon.Models.Agent;
using NextHorizon.Services;
using System.Net;
using System.Text.RegularExpressions;

namespace NextHorizon.Controllers;

public sealed class AgentController : Controller
{
    private const string SupportAgentRole = "Support Agent";
    private readonly IAgentDashboardService _agentDashboardService;
    private readonly IWebHostEnvironment _environment;

    public AgentController(IAgentDashboardService agentDashboardService, IWebHostEnvironment environment)
    {
        _agentDashboardService = agentDashboardService;
        _environment = environment;
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

        HttpContext.Session.Clear();
        TempData["LoginError"] = "Agent workspace access is restricted to Support Agent users only.";
        context.Result = isApiRequest ? Unauthorized() : RedirectToAction("AdminLogin", "Login");
    }

    public IActionResult AgentDashboard()
    {
        return Content(BuildAgentDashboardHtml(), "text/html");
    }

    public IActionResult Tickets()
    {
        return RedirectToAction(nameof(AgentDashboard));
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

    private string BuildAgentDashboardHtml()
    {
        var viewPath = Path.Combine(_environment.ContentRootPath, "Views", "Agent", "AgentDashboard.cshtml");
        var body = System.IO.File.ReadAllText(viewPath);
        var sectionStart = body.IndexOf("<section", StringComparison.OrdinalIgnoreCase);
        body = sectionStart >= 0 ? body[sectionStart..] : body;
        var fullName = HttpContext.Session.GetString("FullName")?.Trim();
        var userName = HttpContext.Session.GetString("Username")?.Trim();
        var displayName = string.IsNullOrWhiteSpace(fullName) ? userName : fullName;
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = "Agent";
        }
        displayName = WebUtility.HtmlEncode(displayName);

        var welcomeHtml = $"""
        <section class="agent-welcome-message" aria-label="Welcome">
            <div class="agent-welcome-kicker">Agent workspace</div>
            <h2>Welcome, {displayName}</h2>
            <p>Here is your monthly performance view, queue history, and scorecard access.</p>
        </section>
        """;
        body = body.Replace("<div class=\"hero-container\">", welcomeHtml + "<div class=\"hero-container\">", StringComparison.Ordinal);
        body = body.Replace("href=\"~/", "href=\"/", StringComparison.Ordinal);
        body = body.Replace("src=\"~/", "src=\"/", StringComparison.Ordinal);
        body = Regex.Replace(body, @"^\s*<link\s+rel=""stylesheet""[^>]*AgentDashboard\.css""\s*/>\s*", string.Empty);

        return $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="UTF-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1.0" />
            <title>Agent Dashboard - NEXT HORIZON AGENT</title>
            <link rel="preconnect" href="https://fonts.googleapis.com" />
            <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
            <link href="https://fonts.googleapis.com/css2?family=Poppins:wght@400;500;600;700&display=swap" rel="stylesheet" />
            <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" />
            <link rel="stylesheet" href="/css/AgentLayout.css" />
            <link rel="stylesheet" href="/css/AgentDashboard.css" />
        </head>
        <body>
            <div class="agent-shell">
                <header class="agent-header">
                    <div class="agent-brand">
                        <img src="/images/nh-logo.jpg" alt="NEXT HORIZON" class="agent-logo" />
                    </div>
                    <div class="header-actions">
                        <div class="notif-wrap">
                            <button class="hdr-icon-btn" aria-label="Notifications" title="Notifications" type="button">
                                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                    <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"></path>
                                    <path d="M13.73 21a2 2 0 0 1-3.46 0"></path>
                                </svg>
                                <span class="notif-dot"></span>
                            </button>
                        </div>

                        <div class="profile-dropdown-wrap" id="profile-wrap">
                            <button class="hdr-icon-btn" id="profile-btn" onclick="toggleProfileDropdown()" aria-label="Account" aria-expanded="false" title="Account" type="button">
                                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                    <path d="M20 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"></path>
                                    <circle cx="12" cy="7" r="4"></circle>
                                </svg>
                            </button>
                            <div class="profile-dropdown" id="profile-dropdown">
                                <a href="/Login/Logout" class="profile-dropdown-item profile-dd-logout">
                                    <i class="bi bi-box-arrow-right"></i> Logout
                                </a>
                            </div>
                        </div>
                    </div>
                </header>

                <div class="agent-workspace">
                    <div class="hamburger-trigger" id="hamburger-trigger" aria-label="Open Sidebar">
                        <i class="bi bi-list"></i>
                    </div>

                    <aside class="agent-sidebar" id="agent-sidebar" aria-label="Agent navigation">
                        <p class="agent-side-title">Agent Workspace</p>
                        <nav class="agent-side-nav">
                            <a href="/Agent/AgentDashboard" class="agent-side-link active">
                                <i class="bi bi-grid-1x2"></i>
                                <span class="nav-text">Dashboard</span>
                            </a>
                        </nav>
                    </aside>

                    <main class="agent-main">
                        {{body}}
                    </main>
                </div>
            </div>

            <script>
                function toggleProfileDropdown() {
                    var dd = document.getElementById('profile-dropdown');
                    if (!dd) return;
                    dd.classList.toggle('open');
                }

                document.addEventListener('click', function (e) {
                    var wrap = document.getElementById('profile-wrap');
                    if (!wrap) return;
                    if (!wrap.contains(e.target)) {
                        var dd = document.getElementById('profile-dropdown');
                        if (dd) dd.classList.remove('open');
                    }
                });

                document.addEventListener('keydown', function (e) {
                    if (e.key === 'Escape') {
                        var dd = document.getElementById('profile-dropdown');
                        if (dd) dd.classList.remove('open');
                    }
                });

                document.addEventListener('DOMContentLoaded', function () {
                    const sidebar = document.getElementById('agent-sidebar');
                    const trigger = document.getElementById('hamburger-trigger');
                    if (!sidebar || !trigger) return;

                    let hideTimeout;

                    function expandSidebar() {
                        clearTimeout(hideTimeout);
                        sidebar.classList.remove('collapsed');
                        trigger.classList.add('hidden');
                    }

                    function startCollapseTimer() {
                        clearTimeout(hideTimeout);
                        hideTimeout = setTimeout(function () {
                            sidebar.classList.add('collapsed');
                            trigger.classList.remove('hidden');
                        }, 4000);
                    }

                    trigger.addEventListener('mouseenter', expandSidebar);
                    trigger.addEventListener('mousemove', expandSidebar);
                    sidebar.addEventListener('mouseenter', expandSidebar);
                    sidebar.addEventListener('mousemove', expandSidebar);
                    sidebar.addEventListener('mouseleave', startCollapseTimer);
                    trigger.addEventListener('mouseleave', startCollapseTimer);
                    startCollapseTimer();
                });
            </script>
        </body>
        </html>
        """;
    }
}
