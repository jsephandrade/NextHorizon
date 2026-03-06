using Microsoft.AspNetCore.Mvc;

namespace MyAspNetApp.Controllers
{
    public class LeaderboardController : Controller
    {
        [HttpGet]
        public IActionResult LeaderboardDashboard()
        {
            return View();
        }
    }
}
