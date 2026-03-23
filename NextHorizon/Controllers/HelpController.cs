using Microsoft.AspNetCore.Mvc;

namespace YourNamespace.Controllers
{
    [Route("Help")]
    public class HelpController : Controller
    {
        [HttpGet("")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("Order")]
        public IActionResult Order()
        {
            return View();
        }

        [HttpGet("Tracking")]
        public IActionResult Tracking()
        {
            return View();
        }

        [HttpGet("Billing")]
        public IActionResult Billing()
        {
            return View();
        }

        [HttpGet("Account")]
        public IActionResult Account()
        {
            return View();
        }

        [HttpGet("Returns")]
        public IActionResult Returns()
        {
            return View();
        }

        [HttpGet("Technical")]
        public IActionResult Technical()
        {
            return View();
        }

        [HttpGet("Contact")]
        public IActionResult Contact()
        {
            return View();
        }
    }
}