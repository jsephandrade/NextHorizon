using Microsoft.AspNetCore.Mvc;
using NextHorizon.Models;
using System.Diagnostics;

namespace NextHorizon.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return RedirectToAction("AdminLogin", "Login");
    }

    public IActionResult Privacy()
    {
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult About()
    {
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Contact()
    {
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Shop()
    {
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Product(int id)
    {
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult SellerShop(int id)
    {
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/consumer/messenger/")]
    public IActionResult ConsumerMessenger(int? sellerId, int? conversationId, int? orderId)
    {
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Reviews(int id)
    {
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult WriteReview(int id)
    {
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Cart()
    {
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Checkout()
    {
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult PlaceOrder(CheckoutViewModel model)
    {
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult MyOrders()
    {
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult OrderConfirmation(string? id)
    {
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult OrderDetail(string? id)
    {
        return RedirectToAction(nameof(Index));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
