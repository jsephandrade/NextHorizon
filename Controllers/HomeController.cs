using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAspNetApp.Data;
using MyAspNetApp.Models;
using System.Text.Json;

namespace MyAspNetApp.Controllers
{
    public record UpdateShippingRequest(
        string OrderNumber,
        string? ReceiverName,
        string? Phone,
        string? Address,
        string? PaymentMethod
    );

    public class HomeController : Controller
    {
        private readonly AppDbContext _db;

        public HomeController(AppDbContext db)
        {
            _db = db;
        }

        // GET: /
        public IActionResult Index() => View();

        // GET: /Shop
        public IActionResult Shop() => View();

        // GET: /Home/About
        public IActionResult About() => View();

        // GET: /Home/Product?id=5
        public IActionResult Product(int id)
        {
            ViewData["ProductId"] = id;
            return View();
        }

        // GET: /Home/Contact
        public IActionResult Contact() => View();

        // GET: /Home/Reviews?id=5
        public IActionResult Reviews(int id)
        {
            ViewData["ProductId"] = id;
            return View();
        }

        // GET: /Home/WriteReview?id=5
        public IActionResult WriteReview(int id)
        {
            ViewData["ProductId"] = id;
            return View();
        }

        // GET: /Home/Cart
        public IActionResult Cart() => View();

        // GET: /Home/Checkout
        public async Task<IActionResult> Checkout()
        {
            if (!ProductData.Cart.Any())
                return RedirectToAction("Cart");

            var vm = await BuildCheckoutVmAsync();
            return View(vm);
        }

        // POST: /Home/PlaceOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel vm)
        {
            vm.CartItems = await GetCheckoutItemsAsync();
            vm.Subtotal  = vm.CartItems.Sum(i => i.Price * i.Quantity);
            vm.ShippingFee = vm.DeliveryOption == "Express" ? 300m : 150m;

            ModelState.Remove("CartItems");
            ModelState.Remove("Subtotal");
            ModelState.Remove("ShippingFee");
            ModelState.Remove("CardNumber");
            ModelState.Remove("CardExpiry");
            ModelState.Remove("CardCvv");

            if (!ModelState.IsValid)
                return View("Checkout", vm);

            var estimatedDelivery = DateTime.Now.AddDays(vm.DeliveryOption == "Express" ? 2 : 5);

            // ── Save order header to dbo.Orders ──
            var dbOrder = new DbOrder
            {
                ConsumerID           = HttpContext.Session.GetInt32("ConsumerId"),
                OrderDate            = DateTime.Now,
                Quantity             = vm.CartItems.Sum(i => i.Quantity),
                SubTotal             = vm.Subtotal,
                ShippingFee          = vm.ShippingFee,
                TotalAmount          = vm.Subtotal + vm.ShippingFee,
                Status               = "Placed",
                FullName             = vm.FullName,
                Email                = vm.Email,
                PhoneNumber          = vm.Phone,
                StreetAddress        = vm.Address,
                City                 = vm.City,
                PostalCode           = vm.PostalCode,
                DeliveryOption       = vm.DeliveryOption,
                PaymentMethod        = vm.PaymentMethod,
                EstimatedDeliveryDate = estimatedDelivery
            };

            _db.Orders.Add(dbOrder);
            await _db.SaveChangesAsync();   // generates OrderID

            // ── Save order lines to dbo.OrderItems ──
            foreach (var item in vm.CartItems)
            {
                _db.OrderItems.Add(new DbOrderItem
                {
                    OrderID   = dbOrder.OrderID,
                    ProductId = item.ProductId,
                    Quantity  = item.Quantity,
                    UnitPrice = item.Price,
                    Size      = item.Size
                });
            }
            await _db.SaveChangesAsync();

            ProductData.Cart.Clear();

            var confirmVm = MapDbOrderToVm(dbOrder, vm.CartItems);

            TempData["OrderConfirmation"] = JsonSerializer.Serialize(confirmVm);
            return RedirectToAction("OrderConfirmation");
        }

        // GET: /Home/OrderConfirmation
        public IActionResult OrderConfirmation()
        {
            if (TempData["OrderConfirmation"] is not string json)
                return RedirectToAction("Cart");

            var vm = JsonSerializer.Deserialize<OrderConfirmationViewModel>(json);
            if (vm == null) return RedirectToAction("Cart");

            TempData.Keep("OrderConfirmation");
            return View(vm);
        }

        // GET: /Home/MyOrders
        public async Task<IActionResult> MyOrders()
        {
            var consumerId = HttpContext.Session.GetInt32("ConsumerId");
            var dbOrders = consumerId.HasValue
                ? await _db.Orders
                    .Where(o => o.ConsumerID == consumerId.Value)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync()
                : new List<DbOrder>();

            var orders = dbOrders.Select(o => MapDbOrderToVm(o, new List<CheckoutItem>())).ToList();
            return View(orders);
        }

        // GET: /Home/MyPurchases
        public async Task<IActionResult> MyPurchases(string? status)
        {
            var consumerId = HttpContext.Session.GetInt32("ConsumerId");
            var dbOrders = consumerId.HasValue
                ? await _db.Orders
                    .Where(o => o.ConsumerID == consumerId.Value)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync()
                : new List<DbOrder>();

            var orderIds = dbOrders.Select(o => o.OrderID).ToList();
            var allItems = await _db.OrderItems
                .Where(i => orderIds.Contains(i.OrderID))
                .ToListAsync();

            var productIds = allItems.Select(i => i.ProductId).Distinct().ToList();
            var products   = await _db.Products
                .Where(p => productIds.Contains(p.ProductId))
                .ToListAsync();

            var orders = dbOrders.Select(o =>
            {
                var firstItem = allItems.FirstOrDefault(i => i.OrderID == o.OrderID);
                var product   = firstItem != null ? products.FirstOrDefault(p => p.ProductId == firstItem.ProductId) : null;

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
                    ProductImage    = product?.ImagePath ?? "",
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

            return View(orders);
        }

        // GET: /Home/OrderDetail?id=ORD-5
        public async Task<IActionResult> OrderDetail(string? id)
        {
            if (!string.IsNullOrEmpty(id) && id.StartsWith("ORD-") &&
                int.TryParse(id["ORD-".Length..], out int numericId))
            {
                var dbOrder = await _db.Orders.FindAsync(numericId);
                if (dbOrder != null)
                {
                    var dbItems = await _db.OrderItems
                        .Where(i => i.OrderID == numericId)
                        .ToListAsync();

                    var productIds = dbItems.Select(i => i.ProductId).Distinct().ToList();
                    var products   = await _db.Products
                        .Where(p => productIds.Contains(p.ProductId))
                        .ToListAsync();

                    var checkoutItems = dbItems.Select(i =>
                    {
                        var p = products.FirstOrDefault(pr => pr.ProductId == i.ProductId);
                        return new CheckoutItem
                        {
                            ProductId = i.ProductId,
                            Name      = p?.ProductName ?? "Unknown",
                            Image     = p?.ImagePath ?? "",
                            Size      = i.Size ?? "",
                            Price     = i.UnitPrice,
                            Quantity  = i.Quantity
                        };
                    }).ToList();

                    return View(MapDbOrderToVm(dbOrder, checkoutItems));
                }
            }

            // Fallback: last confirmed order in TempData
            if (TempData["OrderConfirmation"] is string json)
            {
                var vm = JsonSerializer.Deserialize<OrderConfirmationViewModel>(json);
                if (vm != null) return View(vm);
            }

            return RedirectToAction("MyOrders");
        }

        // POST: /Home/CancelOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(string orderId, string cancellationReason)
        {
            if (!string.IsNullOrEmpty(orderId) && orderId.StartsWith("ORD-") &&
                int.TryParse(orderId["ORD-".Length..], out int numericId))
            {
                var dbOrder = await _db.Orders.FindAsync(numericId);
                if (dbOrder != null)
                {
                    if (dbOrder.Status != "Placed")
                    {
                        TempData["ErrorMessage"] = "This order can no longer be cancelled because it has already been processed.";
                        return RedirectToAction("OrderDetail", new { id = orderId });
                    }

                    dbOrder.Status = "Cancelled";
                    dbOrder.CancellationReason = cancellationReason?.Trim();
                    await _db.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Your order has been successfully cancelled.";
                    return RedirectToAction("OrderDetail", new { id = orderId });
                }
            }

            TempData["ErrorMessage"] = "Order not found.";
            return RedirectToAction("MyOrders");
        }

        // POST: /Home/UpdateOrderShipping
        [HttpPost]
        public async Task<IActionResult> UpdateOrderShipping(
            [FromBody] UpdateShippingRequest req)
        {
            if (req == null || string.IsNullOrEmpty(req.OrderNumber))
                return BadRequest(new { success = false, message = "Invalid request." });

            if (!req.OrderNumber.StartsWith("ORD-") ||
                !int.TryParse(req.OrderNumber["ORD-".Length..], out int numericId))
                return BadRequest(new { success = false, message = "Invalid order number." });

            var dbOrder = await _db.Orders.FindAsync(numericId);
            if (dbOrder == null)
                return NotFound(new { success = false, message = "Order not found." });

            if (dbOrder.Status != "Placed")
                return BadRequest(new { success = false, message = "Shipping details cannot be changed once the order is being processed." });

            dbOrder.FullName      = req.ReceiverName?.Trim() ?? dbOrder.FullName;
            dbOrder.PhoneNumber   = req.Phone?.Trim()        ?? dbOrder.PhoneNumber;
            dbOrder.StreetAddress = req.Address?.Trim()      ?? dbOrder.StreetAddress;
            dbOrder.PaymentMethod = req.PaymentMethod?.Trim() ?? dbOrder.PaymentMethod;

            await _db.SaveChangesAsync();
            return Ok(new { success = true, message = "Shipping details updated successfully." });
        }

        // GET: /Home/Wishlist
        public IActionResult Wishlist() => View();

        // GET: /Home/SellerShop?id=1
        public IActionResult SellerShop(int id)
        {
            ViewData["SellerId"] = id;
            return View();
        }

        // GET: /Home/Error
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View();

        // ── Private helpers ──────────────────────────────────────────────────

        private async Task<List<CheckoutItem>> GetCheckoutItemsAsync()
        {
            var productIds = ProductData.Cart.Select(c => c.ProductId).Distinct().ToList();

            var dbProducts = await _db.Products
                .Where(p => productIds.Contains(p.ProductId))
                .ToListAsync();

            return ProductData.Cart
                .Select(ci =>
                {
                    var p = dbProducts.FirstOrDefault(d => d.ProductId == ci.ProductId);
                    if (p == null) return null;

                    decimal price = p.Discount.HasValue && p.Discount.Value > 0
                        ? p.Price - p.Discount.Value
                        : p.Price;

                    return new CheckoutItem
                    {
                        ProductId = ci.ProductId,
                        Name      = p.ProductName,
                        Image     = p.ImagePath ?? "",
                        Size      = ci.Size,
                        Price     = price,
                        Quantity  = ci.Quantity
                    };
                })
                .Where(i => i != null)
                .Select(i => i!)
                .ToList();
        }

        private async Task<CheckoutViewModel> BuildCheckoutVmAsync()
        {
            var items = await GetCheckoutItemsAsync();
            return new CheckoutViewModel
            {
                CartItems   = items,
                Subtotal    = items.Sum(i => i.Price * i.Quantity),
                ShippingFee = 150m,
            };
        }

        private static OrderConfirmationViewModel MapDbOrderToVm(DbOrder o, List<CheckoutItem> items)
        {
            return new OrderConfirmationViewModel
            {
                OrderId               = $"ORD-{o.OrderID}",
                OrderStatus           = o.Status,
                FullName              = o.FullName,
                Email                 = o.Email,
                Phone                 = o.PhoneNumber,
                Address               = o.StreetAddress,
                City                  = o.City,
                PostalCode            = o.PostalCode,
                DeliveryOption        = o.DeliveryOption,
                PaymentMethod         = o.PaymentMethod,
                CreatedAt             = o.OrderDate,
                EstimatedDeliveryDate = o.EstimatedDeliveryDate,
                Subtotal              = o.SubTotal,
                ShippingFee           = o.ShippingFee,
                CancellationReason    = o.CancellationReason,
                OrderItems            = items
            };
        }
    }
}
