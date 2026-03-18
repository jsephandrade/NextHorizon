using Microsoft.AspNetCore.Mvc;

namespace YourProjectNamespace.Controllers
{
    public class HelpController : Controller
    {
        // GET: /Help/
        public IActionResult Index()
        {
            return View();
        }
    }
}