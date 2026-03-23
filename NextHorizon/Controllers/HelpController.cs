using Microsoft.AspNetCore.Mvc;

namespace YourProjectNamespace.Controllers
{
    public class HelpController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Orders()
        {
            return View();
        }

        public IActionResult Payouts()
        {
            return View();
        }

        public IActionResult Account()
        {
            return View();
        }

        public IActionResult Shipping()
        {
            return View();
        }

        public IActionResult Marketing()
        {
            return View();
        }
    }
}