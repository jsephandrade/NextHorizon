using Microsoft.AspNetCore.Mvc;
using MemberTracker.Data;
using MemberTracker.Models;
using NextHorizon.Models;
using System.Diagnostics;

namespace NextHorizon.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet]
        public IActionResult About()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Contact()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Shop()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Product(int id)
        {
            ViewData["ProductId"] = id;
            return View();
        }

        [HttpGet]
        public IActionResult SellerShop(int id)
        {
            ViewData["SellerId"] = id;
            return View();
        }

        [HttpGet]
        public IActionResult Reviews(int id)
        {
            ViewData["ProductId"] = id;
            return View();
        }

        [HttpGet]
        public IActionResult WriteReview(int id)
        {
            ViewData["ProductId"] = id;
            return View();
        }

        [HttpGet]
        public IActionResult Cart()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Checkout()
        {
            return View(BuildCheckoutViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PlaceOrder(CheckoutViewModel model)
        {
            model.CartItems = BuildCheckoutItems();
            ApplyCheckoutTotals(model);

            if (model.CartItems.Count == 0)
            {
                TempData["ErrorMessage"] = "Your cart is empty.";
                return View("Checkout", model);
            }

            if (!ModelState.IsValid)
            {
                return View("Checkout", model);
            }

            var order = new OrderConfirmationViewModel
            {
                OrderId = ProductData.CreateOrderId(),
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim(),
                Phone = string.IsNullOrWhiteSpace(model.Phone) ? null : model.Phone.Trim(),
                Address = model.Address.Trim(),
                City = model.City.Trim(),
                PostalCode = model.PostalCode.Trim(),
                DeliveryOption = string.IsNullOrWhiteSpace(model.DeliveryOption) ? "Standard" : model.DeliveryOption.Trim(),
                PaymentMethod = string.IsNullOrWhiteSpace(model.PaymentMethod) ? "Card" : model.PaymentMethod.Trim(),
                OrderStatus = "Placed",
                StatusColor = ResolveStatusColor("Placed"),
                CurrentStep = 0,
                OrderItems = model.CartItems,
                Subtotal = model.Subtotal,
                ShippingFee = model.ShippingFee,
                Total = model.Total,
                CreatedAt = DateTime.UtcNow,
                EstimatedDeliveryDate = DateTime.UtcNow.AddDays(string.Equals(model.DeliveryOption, "Express", StringComparison.OrdinalIgnoreCase) ? 2 : 5),
            };

            ProductData.SaveOrder(order);
            ProductData.Cart.Clear();

            return RedirectToAction(nameof(OrderConfirmation), new { id = order.OrderId });
        }

        [HttpGet]
        public IActionResult MyOrders()
        {
            return View(ProductData.Orders);
        }

        [HttpGet]
        public IActionResult OrderConfirmation(string? id)
        {
            var order = ResolveOrder(id);
            return order is null ? RedirectToAction(nameof(MyOrders)) : View(order);
        }

        [HttpGet]
        public IActionResult OrderDetail(string? id)
        {
            var order = ResolveOrder(id);
            return order is null ? RedirectToAction(nameof(MyOrders)) : View(order);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private static CheckoutViewModel BuildCheckoutViewModel()
        {
            var model = new CheckoutViewModel
            {
                DeliveryOption = "Standard",
                PaymentMethod = "Card",
                CartItems = BuildCheckoutItems(),
            };

            ApplyCheckoutTotals(model);
            return model;
        }

        private static List<CheckoutCartItemViewModel> BuildCheckoutItems()
        {
            return ProductData.Cart
                .Select(item =>
                {
                    var product = ProductData.Products.FirstOrDefault(p => p.Id == item.ProductId);
                    return product is null
                        ? null
                        : new CheckoutCartItemViewModel
                        {
                            ProductId = product.Id,
                            Name = product.Name,
                            Image = product.Image,
                            Size = item.Size,
                            Quantity = item.Quantity,
                            Price = product.Price,
                        };
                })
                .Where(static item => item is not null)
                .Select(static item => item!)
                .ToList();
        }

        private static void ApplyCheckoutTotals(CheckoutViewModel model)
        {
            model.Subtotal = model.CartItems.Sum(item => item.Price * item.Quantity);
            model.ShippingFee = string.Equals(model.DeliveryOption, "Express", StringComparison.OrdinalIgnoreCase) ? 300m : 150m;
            model.Total = model.Subtotal + model.ShippingFee;
        }

        private static OrderConfirmationViewModel? ResolveOrder(string? orderId)
        {
            if (!string.IsNullOrWhiteSpace(orderId))
            {
                return ProductData.Orders.FirstOrDefault(order => string.Equals(order.OrderId, orderId, StringComparison.OrdinalIgnoreCase));
            }

            return ProductData.Orders.FirstOrDefault();
        }

        private static string ResolveStatusColor(string status)
        {
            return status switch
            {
                "Delivered" => "#15803d",
                "Shipped" => "#1d4ed8",
                "Processing" => "#b45309",
                _ => "#0a0a0a",
            };
        }
    }
}
