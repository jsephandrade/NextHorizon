using Microsoft.AspNetCore.Mvc;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.ViewModels;
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