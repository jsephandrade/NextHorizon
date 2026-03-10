using Microsoft.AspNetCore.Mvc;
using MyAspNetApp.Services;
using MyAspNetApp.Models;

namespace MyAspNetApp.Controllers
{
    public class OrderController : Controller
    {
        private readonly OrderService _orderService;

        public OrderController(OrderService orderService)
        {
            _orderService = orderService;
        }

        public IActionResult MyPurchasesOptions()
        {
            var orders = _orderService.GetUserPurchases();
            return View(orders);
        }
    }
}
