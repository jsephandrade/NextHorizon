using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAspNetApp.Data;
using MyAspNetApp.Models;

namespace MyAspNetApp.Controllers
{
    public class AccountProfileController : Controller
    {
        private readonly AppDbContext _db;

        public AccountProfileController(AppDbContext db)
        {
            _db = db;
        }

        // GET: /AccountProfile/ProfileView  → redirects to My Purchases as default profile landing
        public IActionResult ProfileView()
        {
            return RedirectToAction("MyPurchases");
        }

        // GET: /AccountProfile/MyPurchases
        public async Task<IActionResult> MyPurchases(string? status)
        {
            var consumerId = HttpContext.Session.GetInt32("ConsumerId");

            // Fetch only this consumer's orders; if not logged in, show empty list
            var dbOrders = consumerId.HasValue
                ? await _db.Orders
                    .Where(o => o.ConsumerID == consumerId.Value)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync()
                : new List<DbOrder>();

            var orderIds    = dbOrders.Select(o => o.OrderID).ToList();
            var allItems    = await _db.OrderItems
                                       .Where(i => orderIds.Contains(i.OrderID))
                                       .ToListAsync();
            var productIds  = allItems.Select(i => i.ProductId).Distinct().ToList();
            var products    = await _db.Products
                                       .Where(p => productIds.Contains(p.ProductId))
                                       .ToListAsync();

            var orders = dbOrders.Select(o =>
            {
                var firstItem = allItems.FirstOrDefault(i => i.OrderID == o.OrderID);
                var product   = firstItem != null
                    ? products.FirstOrDefault(p => p.ProductId == firstItem.ProductId)
                    : null;

                var mappedStatus = o.Status switch
                {
                    "Placed"     => "To Pay",
                    "Processing" => "To Ship",
                    "Shipped"    => "To Receive",
                    "Delivered"  => "Completed",
                    _            => o.Status
                };

                return new OrderViewModel
                {
                    OrderNumber     = "ORD-" + o.OrderID,
                    Status          = mappedStatus,
                    ProductName     = product?.ProductName ?? "Product",
                    ProductImage    = product?.ImagePath   ?? "",
                    PaymentMethod   = o.PaymentMethod,
                    SellerName      = "Official Store",
                    ReceiverName    = o.FullName,
                    PhoneNumber     = o.PhoneNumber,
                    ShippingAddress = $"{o.StreetAddress}, {o.City} {o.PostalCode}",
                    OrderDate       = o.OrderDate,
                    Size            = firstItem?.Size,
                    Quantity        = o.Quantity,
                    TotalAmount     = o.TotalAmount
                };
            }).ToList();

            if (!string.IsNullOrEmpty(status))
                orders = orders.Where(o => o.Status == status).ToList();

            return View("~/Views/Home/MyPurchases.cshtml", orders);
        }

        // GET: /AccountProfile/UpdateProfile
        public IActionResult UpdateProfile() => View();

        // GET: /AccountProfile/UploadActivity
        public IActionResult UploadActivity() => View();

        // GET: /AccountProfile/ShippingAddress
        public IActionResult ShippingAddress() => View();

        // GET: /AccountProfile/PaymentMethods
        public IActionResult PaymentMethods() => View();
    }
}
