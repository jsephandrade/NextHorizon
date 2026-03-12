using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MemberTracker.Controllers;

[Authorize]
public class MemberController : Controller
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IConfiguration _configuration;

    public MemberController(IWebHostEnvironment webHostEnvironment, IConfiguration configuration)
    {
        _webHostEnvironment = webHostEnvironment;
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult UploadActivity()
    {
        return View("~/Views/MemberTracker/UploadActivity.cshtml");
    }

    [HttpGet]
    public IActionResult MyActivity()
    {
        return View("~/Views/MemberTracker/MyActivity.cshtml");
    }

    [HttpGet]
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
