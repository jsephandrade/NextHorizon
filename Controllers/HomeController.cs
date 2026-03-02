using Microsoft.AspNetCore.Mvc;
using MyAspNetApp.Data;
using MyAspNetApp.Models;
using System.Text.Json;

namespace MyAspNetApp.Controllers
{
    public class HomeController : Controller
    {
        // GET: /
        public IActionResult Index()
        {
            return View();
        }

        // GET: /Shop
        public IActionResult Shop()
        {
            return View();
        }

        // GET: /Home/About
        public IActionResult About()
        {
            return View();
        }

        // GET: /Home/Product?id=5
        public IActionResult Product(int id)
        {
            ViewData["ProductId"] = id;
            return View();
        }

        // GET: /Home/Contact
        public IActionResult Contact()
        {
            return View();
        }

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
        public IActionResult Cart()
        {
            return View();
        }

        // GET: /Home/Checkout
        public IActionResult Checkout()
        {
            if (!ProductData.Cart.Any())
                return RedirectToAction("Cart");

            var vm = BuildCheckoutVm();
            return View(vm);
        }

        // POST: /Home/PlaceOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PlaceOrder(CheckoutViewModel vm)
        {
            // Always re-populate cart items from server data
            vm.CartItems = GetCheckoutItems();
            vm.Subtotal = vm.CartItems.Sum(i => i.Price * i.Quantity);
            vm.ShippingFee = vm.DeliveryOption == "Express" ? 300m : 150m;

            ModelState.Remove("CartItems");
            ModelState.Remove("Subtotal");
            ModelState.Remove("ShippingFee");
            ModelState.Remove("CardNumber");
            ModelState.Remove("CardExpiry");
            ModelState.Remove("CardCvv");

            if (!ModelState.IsValid)
                return View("Checkout", vm);

            foreach (var item in vm.CartItems)
            {
                ProductData.PurchaseRecords.Add(new PurchaseRecord
                {
                    ProductId = item.ProductId,
                    PurchaseDate = DateTime.Now,
                    DeliveryDate = DateTime.Now.AddDays(vm.DeliveryOption == "Express" ? 2 : 5)
                });
            }

            var confirmVm = new OrderConfirmationViewModel
            {
                OrderId = $"ORD-{DateTime.Now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
                OrderStatus = "Placed",
                FullName = vm.FullName,
                Email = vm.Email,
                Phone = vm.Phone,
                Address = vm.Address,
                City = vm.City,
                PostalCode = vm.PostalCode,
                DeliveryOption = vm.DeliveryOption,
                PaymentMethod = vm.PaymentMethod,
                CreatedAt = DateTime.Now,
                EstimatedDeliveryDate = DateTime.Now.AddDays(vm.DeliveryOption == "Express" ? 2 : 5),
                OrderItems = vm.CartItems,
                Subtotal = vm.Subtotal,
                ShippingFee = vm.ShippingFee,
            };

            ProductData.Orders.Insert(0, confirmVm);
            ProductData.Cart.Clear();

            TempData["OrderConfirmation"] = JsonSerializer.Serialize(confirmVm);
            return RedirectToAction("OrderConfirmation");
        }

        // GET: /Home/OrderConfirmation
        public IActionResult OrderConfirmation()
        {
            if (TempData["OrderConfirmation"] is not string json)
                return RedirectToAction("Cart");

            var vm = JsonSerializer.Deserialize<OrderConfirmationViewModel>(json);
            if (vm == null)
                return RedirectToAction("Cart");

            // Keep so OrderDetail can also read it
            TempData.Keep("OrderConfirmation");
            return View(vm);
        }

        // GET: /Home/MyOrders
        public IActionResult MyOrders()
        {
            return View(ProductData.Orders);
        }

        // GET: /Home/OrderDetail?id=ORD-...
        public IActionResult OrderDetail(string? id)
        {
            OrderConfirmationViewModel? vm = null;

            if (!string.IsNullOrEmpty(id))
                vm = ProductData.Orders.FirstOrDefault(o => o.OrderId == id);

            if (vm == null)
            {
                if (TempData["OrderConfirmation"] is string json)
                    vm = System.Text.Json.JsonSerializer.Deserialize<OrderConfirmationViewModel>(json);
            }

            if (vm == null)
                return RedirectToAction("MyOrders");

            return View(vm);
        }

        private List<CheckoutItem> GetCheckoutItems()
        {
            return ProductData.Cart.Select(ci =>
            {
                var product = ProductData.Products.FirstOrDefault(p => p.Id == ci.ProductId);
                return new CheckoutItem
                {
                    ProductId = ci.ProductId,
                    Name = product?.Name ?? "Unknown",
                    Image = product?.Image ?? "",
                    Size = ci.Size,
                    Price = product?.Price ?? 0,
                    Quantity = ci.Quantity
                };
            }).Where(i => i.Price > 0).ToList();
        }

        private CheckoutViewModel BuildCheckoutVm()
        {
            var items = GetCheckoutItems();
            return new CheckoutViewModel
            {
                CartItems = items,
                Subtotal = items.Sum(i => i.Price * i.Quantity),
                ShippingFee = 150m,
            };
        }

        // GET: /Home/Wishlist
        public IActionResult Wishlist()
        {
            return View();
        }

        // GET: /Home/Error
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
