using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NextHorizon.Modules.MemberTracker.Controllers;

[Authorize]
[Route("member-tracker")]
public class MemberTrackerController : Controller
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IConfiguration _configuration;

    public MemberTrackerController(IWebHostEnvironment webHostEnvironment, IConfiguration configuration)
    {
        _webHostEnvironment = webHostEnvironment;
        _configuration = configuration;
    }

    [HttpGet("upload-activity")]
    public IActionResult UploadActivity()
    {
        return View();
    }

    [HttpGet("my-activity")]
    public IActionResult MyActivity()
    {
        return View();
    }

    [HttpGet("dev-messaging")]
    public IActionResult DevMessaging()
    {
        var devMessagingEnabled = _configuration.GetValue("Features:EnableDevMessaging", false);
        if (!_webHostEnvironment.IsDevelopment() || !devMessagingEnabled)
        {
            return NotFound();
        }

        return View("~/Views/Messaging/DevMessaging.cshtml");
    }
}

