using Microsoft.AspNetCore.Mvc;
using MyAspNetApp.Services;

namespace MyAspNetApp.Controllers
{
    public class AccountProfileController : Controller
    {
        private readonly OrderService _orderService;

        public AccountProfileController(OrderService orderService)
        {
            _orderService = orderService;
        }

        public IActionResult ProfileView()
        {
            return View();
        }

        public IActionResult UpdateProfile()
        {
            return View();
        }

        public IActionResult UploadActivity()
        {
            return View();
        }

        public IActionResult ShippingAddress()
        {
            return View();
        }

        public IActionResult PaymentMethods()
        {
            return View();
        }

        public IActionResult MyPurchases()
        {
            var orders = _orderService.GetUserPurchases();
            return View(orders);
        }
    }
}
