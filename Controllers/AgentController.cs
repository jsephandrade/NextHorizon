using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using NextHorizon.Data;
using NextHorizon.Models.Agent;
using NextHorizon.Services;
using System.Net;
using System.Text.RegularExpressions;

namespace NextHorizon.Controllers;

public sealed class AgentController : Controller
{
    private const string SupportAgentRole = "Support Agent";
    private readonly IAgentDashboardService _agentDashboardService;
    private readonly ApplicationDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;

    public AgentController(IAgentDashboardService agentDashboardService, ApplicationDbContext dbContext, IWebHostEnvironment environment)
    {
        _agentDashboardService = agentDashboardService;
        _dbContext = dbContext;
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

    [HttpGet("/Agent/api/notifications")]
    public async Task<IActionResult> Notifications(CancellationToken cancellationToken)
    {
        var agentUserId = HttpContext.Session.GetInt32("UserId");
        if (!agentUserId.HasValue || agentUserId.Value <= 0)
        {
            return Unauthorized();
        }

        var items = await _dbContext.Notifications
            .AsNoTracking()
            .Where(item => item.RecipientId == agentUserId.Value
                && item.Category == "QAScore")
            .OrderByDescending(item => item.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        var payload = items
            .Select(item =>
            {
                var supportFaqId = ExtractSupportFaqId(item.Message);
                var targetUrl = supportFaqId.HasValue
                    ? $"/Agent/AgentDashboard?ticketId={supportFaqId.Value}"
                    : "/Agent/AgentDashboard";

                return new AgentNotificationItem(
                    item.NotificationId,
                    item.Message,
                    item.IsRead,
                    item.Category,
                    item.CreatedAt.ToLocalTime().ToString("MMM dd, yyyy hh:mm tt"),
                    supportFaqId,
                    targetUrl);
            })
            .ToList();

        return Json(payload);
    }

    [HttpPost("/Agent/api/notifications/{notificationId:int}/read")]
    public async Task<IActionResult> MarkNotificationRead(int notificationId, CancellationToken cancellationToken)
    {
        var agentUserId = HttpContext.Session.GetInt32("UserId");
        if (!agentUserId.HasValue || agentUserId.Value <= 0)
        {
            return Unauthorized();
        }

        var notification = await _dbContext.Notifications
            .FirstOrDefaultAsync(item => item.NotificationId == notificationId
                && item.RecipientId == agentUserId.Value
                && item.Category == "QAScore", cancellationToken);

        if (notification is null)
        {
            return NotFound();
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Ok(new { success = true });
    }

    [HttpPost("/Agent/api/notifications/read-all")]
    public async Task<IActionResult> MarkAllNotificationsRead(CancellationToken cancellationToken)
    {
        var agentUserId = HttpContext.Session.GetInt32("UserId");
        if (!agentUserId.HasValue || agentUserId.Value <= 0)
        {
            return Unauthorized();
        }

        var notifications = await _dbContext.Notifications
            .Where(item => item.RecipientId == agentUserId.Value
                && item.Category == "QAScore"
                && !item.IsRead)
            .ToListAsync(cancellationToken);

        if (notifications.Count > 0)
        {
            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Ok(new { success = true, count = notifications.Count });
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
        var agentLayoutCssPath = Path.Combine(_environment.WebRootPath, "css", "AgentLayout.css");
        var agentDashboardCssPath = Path.Combine(_environment.WebRootPath, "css", "AgentDashboard.css");
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
        var agentLayoutCssVersion = GetAssetVersion(agentLayoutCssPath);
        var agentDashboardCssVersion = GetAssetVersion(agentDashboardCssPath);

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
            <link rel="stylesheet" href="/css/AgentLayout.css?v={{agentLayoutCssVersion}}" />
            <link rel="stylesheet" href="/css/AgentDashboard.css?v={{agentDashboardCssVersion}}" />
        </head>
        <body>
            <div class="agent-shell">
                <header class="agent-header">
                    <div class="agent-brand">
                        <img src="/images/nh-logo.jpg" alt="NEXT HORIZON" class="agent-logo" />
                    </div>
                    <div class="header-actions">
                        <div class="notif-wrap" id="agent-notif-wrap">
                            <button class="hdr-icon-btn" id="agent-notif-btn" aria-label="Notifications" title="Notifications" type="button" aria-expanded="false">
                                <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                                    <path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9"></path>
                                    <path d="M13.73 21a2 2 0 0 1-3.46 0"></path>
                                </svg>
                                <span class="notif-dot" id="agent-notif-dot" hidden></span>
                            </button>
                            <div class="agent-notif-dropdown" id="agent-notif-dropdown" aria-hidden="true">
                                <div class="agent-notif-header">
                                    <span>Notifications</span>
                                    <button type="button" class="agent-notif-read-all" id="agent-notif-read-all">Mark all as read</button>
                                </div>
                                <div class="agent-notif-list" id="agent-notif-list">
                                    <div class="agent-notif-empty">Loading notifications...</div>
                                </div>
                            </div>
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
                const agentNotifEndpoint = '/Agent/api/notifications';
                const agentNotifReadAllEndpoint = '/Agent/api/notifications/read-all';
                const agentNotifRefreshIntervalMs = 5000;
                const agentNotifPreviewDurationMs = 3000;
                let agentNotifRefreshTimerId = 0;
                let agentNotifPreviewTimeoutId = 0;
                let hasLoadedAgentNotifications = false;
                let knownAgentNotificationIds = new Set();

                function toggleProfileDropdown() {
                    var dd = document.getElementById('profile-dropdown');
                    if (!dd) return;
                    dd.classList.toggle('open');
                }

                function formatNotifState(isOpen) {
                    var dropdown = document.getElementById('agent-notif-dropdown');
                    var button = document.getElementById('agent-notif-btn');
                    if (dropdown) {
                        dropdown.classList.toggle('open', !!isOpen);
                        dropdown.setAttribute('aria-hidden', isOpen ? 'false' : 'true');
                    }
                    if (button) {
                        button.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
                    }
                }

                function clearAgentNotificationPreview() {
                    var dropdown = document.getElementById('agent-notif-dropdown');
                    var button = document.getElementById('agent-notif-btn');

                    if (agentNotifPreviewTimeoutId) {
                        window.clearTimeout(agentNotifPreviewTimeoutId);
                        agentNotifPreviewTimeoutId = 0;
                    }

                    if (dropdown) {
                        dropdown.classList.remove('auto-peek');
                    }

                    if (button) {
                        button.classList.remove('notif-attention');
                    }
                }

                function showAgentNotificationPreview() {
                    var dropdown = document.getElementById('agent-notif-dropdown');
                    var button = document.getElementById('agent-notif-btn');
                    if (!dropdown || !button || document.hidden) {
                        return;
                    }

                    clearAgentNotificationPreview();
                    button.classList.add('notif-attention');
                    dropdown.classList.add('auto-peek');
                    formatNotifState(true);

                    agentNotifPreviewTimeoutId = window.setTimeout(function () {
                        clearAgentNotificationPreview();
                        formatNotifState(false);
                    }, agentNotifPreviewDurationMs);
                }

                function escapeHtml(value) {
                    return String(value || '')
                        .replace(/&/g, '&amp;')
                        .replace(/</g, '&lt;')
                        .replace(/>/g, '&gt;')
                        .replace(/"/g, '&quot;')
                        .replace(/'/g, '&#39;');
                }

                async function loadAgentNotifications() {
                    var list = document.getElementById('agent-notif-list');
                    var dot = document.getElementById('agent-notif-dot');
                    var markAllButton = document.getElementById('agent-notif-read-all');
                    if (!list) return;

                    list.innerHTML = '<div class="agent-notif-empty">Loading notifications...</div>';

                    try {
                        var response = await fetch(agentNotifEndpoint, {
                            headers: { 'Accept': 'application/json' }
                        });

                        if (!response.ok) {
                            throw new Error('Notification request failed with status ' + response.status);
                        }

                        var items = await response.json();
                        var notificationItems = Array.isArray(items) ? items : [];
                        var currentNotificationIds = new Set(notificationItems.map(function (item) { return item.notificationId; }));
                        var hasNewNotification = hasLoadedAgentNotifications
                            && notificationItems.some(function (item) { return !knownAgentNotificationIds.has(item.notificationId); });

                        if (notificationItems.length === 0) {
                            list.innerHTML = '<div class="agent-notif-empty">No QA score notifications.</div>';
                            if (dot) dot.hidden = true;
                            if (markAllButton) markAllButton.disabled = true;
                            knownAgentNotificationIds = currentNotificationIds;
                            hasLoadedAgentNotifications = true;
                            return;
                        }

                        list.innerHTML = notificationItems.map(function (item) {
                            return `
                                <a class="agent-notif-item${item.isRead ? '' : ' unread'}" data-notification-id="${item.notificationId}" href="${escapeHtml(item.targetUrl || '/Agent/AgentDashboard')}">
                                    <div class="agent-notif-body">
                                        <div class="agent-notif-category">${escapeHtml(item.category || 'QAScore')}</div>
                                        <div class="agent-notif-text">${escapeHtml(item.message)}</div>
                                        <div class="agent-notif-time">${escapeHtml(item.createdAtLabel)}</div>
                                    </div>
                                </a>
                            `;
                        }).join('');

                        if (dot) {
                            dot.hidden = !notificationItems.some(function (item) { return !item.isRead; });
                        }
                        if (markAllButton) {
                            markAllButton.disabled = !notificationItems.some(function (item) { return !item.isRead; });
                        }

                        if (hasNewNotification) {
                            showAgentNotificationPreview();
                        }

                        knownAgentNotificationIds = currentNotificationIds;
                        hasLoadedAgentNotifications = true;
                    } catch (error) {
                        list.innerHTML = '<div class="agent-notif-empty">Unable to load notifications.</div>';
                        if (dot) dot.hidden = true;
                        if (markAllButton) markAllButton.disabled = true;
                        console.error(error);
                    }
                }

                async function postNotificationAction(url) {
                    var response = await fetch(url, {
                        method: 'POST',
                        headers: { 'Accept': 'application/json' }
                    });

                    if (!response.ok) {
                        throw new Error('Notification update failed with status ' + response.status);
                    }
                }

                function startAgentNotificationAutoRefresh() {
                    if (agentNotifRefreshTimerId) {
                        window.clearInterval(agentNotifRefreshTimerId);
                    }

                    agentNotifRefreshTimerId = window.setInterval(function () {
                        if (document.hidden) {
                            return;
                        }

                        loadAgentNotifications();
                    }, agentNotifRefreshIntervalMs);
                }

                document.addEventListener('click', function (e) {
                    var wrap = document.getElementById('profile-wrap');
                    if (!wrap) return;
                    if (!wrap.contains(e.target)) {
                        var dd = document.getElementById('profile-dropdown');
                        if (dd) dd.classList.remove('open');
                    }

                    var notifWrap = document.getElementById('agent-notif-wrap');
                    if (notifWrap && !notifWrap.contains(e.target)) {
                        formatNotifState(false);
                    }
                });

                document.addEventListener('keydown', function (e) {
                    if (e.key === 'Escape') {
                        var dd = document.getElementById('profile-dropdown');
                        if (dd) dd.classList.remove('open');
                        formatNotifState(false);
                    }
                });

                document.addEventListener('DOMContentLoaded', function () {
                    const sidebar = document.getElementById('agent-sidebar');
                    const trigger = document.getElementById('hamburger-trigger');
                    const notifButton = document.getElementById('agent-notif-btn');
                    const notifList = document.getElementById('agent-notif-list');
                    const notifReadAllButton = document.getElementById('agent-notif-read-all');

                    if (sidebar && trigger) {
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
                    }

                    if (notifButton) {
                        notifButton.addEventListener('click', function () {
                            clearAgentNotificationPreview();
                            var dropdown = document.getElementById('agent-notif-dropdown');
                            var isOpen = dropdown && dropdown.classList.contains('open');
                            formatNotifState(!isOpen);
                        });
                    }

                    if (notifList) {
                        notifList.addEventListener('click', async function (event) {
                            var link = event.target.closest('.agent-notif-item');
                            if (!link) return;

                            var notificationId = link.getAttribute('data-notification-id');
                            if (!notificationId || !link.classList.contains('unread')) {
                                return;
                            }

                            event.preventDefault();

                            try {
                                await postNotificationAction('/Agent/api/notifications/' + notificationId + '/read');
                                link.classList.remove('unread');
                                await loadAgentNotifications();
                            } catch (error) {
                                console.error(error);
                            } finally {
                                window.location.href = link.href;
                            }
                        });
                    }

                    if (notifReadAllButton) {
                        notifReadAllButton.addEventListener('click', async function () {
                            notifReadAllButton.disabled = true;
                            notifReadAllButton.textContent = 'Marking...';

                            try {
                                await postNotificationAction(agentNotifReadAllEndpoint);
                                await loadAgentNotifications();
                            } catch (error) {
                                console.error(error);
                            } finally {
                                notifReadAllButton.textContent = 'Mark all as read';
                            }
                        });
                    }

                    loadAgentNotifications();
                    startAgentNotificationAutoRefresh();
                });

                window.addEventListener('focus', function () {
                    loadAgentNotifications();
                });

                document.addEventListener('visibilitychange', function () {
                    if (!document.hidden) {
                        loadAgentNotifications();
                    }
                });
            </script>
        </body>
        </html>
        """;
    }

    private static string GetAssetVersion(string assetPath)
    {
        return System.IO.File.Exists(assetPath)
            ? System.IO.File.GetLastWriteTimeUtc(assetPath).Ticks.ToString()
            : "1";
    }

    private static int? ExtractSupportFaqId(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        var match = Regex.Match(message, @"ticket\s*#\s*(\d+)", RegexOptions.IgnoreCase);
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out var supportFaqId))
        {
            return null;
        }

        return supportFaqId;
    }
}
