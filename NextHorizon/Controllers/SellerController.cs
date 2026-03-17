using System.Security.Claims;
using NextHorizon.Data;
using NextHorizon.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NextHorizon.Controllers;

[Authorize]
public sealed class SellerController : Controller
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IConfiguration _configuration;
    private readonly IAuthenticatedUserContextService _authenticatedUserContextService;

    public SellerController(
        IWebHostEnvironment webHostEnvironment,
        IConfiguration configuration,
        IAuthenticatedUserContextService authenticatedUserContextService)
    {
        _webHostEnvironment = webHostEnvironment;
        _configuration = configuration;
        _authenticatedUserContextService = authenticatedUserContextService;
    }

    [HttpGet("seller/messenger")]
    public async Task<IActionResult> SellerMessenger(
        string? mode,
        int? actorUserId,
        int? conversationId,
        int? consumerId,
        int? customerId,
        int? orderId,
        CancellationToken cancellationToken)
    {
        var requestedMode = string.Equals(mode, "dev", StringComparison.OrdinalIgnoreCase) ? "dev" : "main";
        var devMessagingEnabled = _webHostEnvironment.IsDevelopment() &&
            _configuration.GetValue("Features:EnableDevMessaging", false);
        var currentUserId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsedUserId)
            ? parsedUserId
            : 0;
        var currentUser = await _authenticatedUserContextService.GetCurrentAsync(User, cancellationToken);
        var sellerName = currentUser?.SellerId is int sellerId
            ? ProductData.Sellers.FirstOrDefault(item => item.Id == sellerId)?.ShopName ?? "Seller"
            : "Seller";

        ViewData["ApiMode"] = requestedMode == "dev" && devMessagingEnabled ? "dev" : "main";
        ViewData["ActorUserId"] = actorUserId ?? currentUserId;
        ViewData["ConversationId"] = conversationId;
        ViewData["ConsumerId"] = consumerId ?? customerId;
        ViewData["OrderId"] = orderId;
        ViewData["DebugUserId"] = actorUserId;
        ViewData["DevMessagingEnabled"] = devMessagingEnabled;
        ViewData["DashboardHeading"] = "Messages";
        ViewData["SellerName"] = sellerName;

        return View("~/Views/Seller/SellerMessenger.cshtml");
    }
}
