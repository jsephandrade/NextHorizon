using Microsoft.AspNetCore.Mvc;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.ViewModels;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Http;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        // 1. You likely need a constructor to inject your _context
        private readonly ApplicationDbContext _context;
        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 2. This was the block causing the error
    public IActionResult Index()
{
    // 1. Fetch products from database
    var dbProducts = _context.Products
                             .OrderBy(p => p.ProductId)
                             .Take(50)
                             .ToList();

    // 2. Convert DbProduct -> Product
    var products = dbProducts.Select(p => new Product
    {
        Id = p.ProductId,
        Name = p.ProductName,
        Description = p.Details ?? "",
        Price = p.Price,
        Image = p.ImagePath ?? "",
        Stock = p.Stock,
        Brand = p.Brand ?? "",
        Category = p.Category ?? ""
    }).ToList();

    // 3. Send to ViewModel
    var viewModel = new LandingPageViewModel
    {
        ShowLoginWall = string.IsNullOrEmpty(HttpContext.Session.GetString("UserType")),
        Products = products
    };

    return View(viewModel);
}

        public IActionResult Login() => RedirectToAction("Login", "Account");
        public IActionResult Signup() => RedirectToAction("Register", "Account");
        public IActionResult BecomeSeller() => View();
        public IActionResult ProductLanding()
        {
            if (IsSeller())
            {
                return RedirectToAction("Index", "Seller");
            }

            return View();
        }

        public IActionResult Cart()
        {
            if (!IsConsumer())
            {
                return RedirectToRoleHome();
            }

            return View();
        }

        [HttpGet]
        public IActionResult Checkout()
        {
            if (!IsConsumer())
            {
                return RedirectToRoleHome();
            }

            var model = BuildCheckoutViewModel();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PlaceOrder(CheckoutViewModel model)
        {
            if (!IsConsumer())
            {
                return RedirectToRoleHome();
            }

            var checkoutData = BuildCheckoutViewModel();
            model.CartItems = checkoutData.CartItems;
            model.Subtotal = checkoutData.Subtotal;
            model.ShippingFee = ResolveShippingFee(model.DeliveryOption);

            if (!ModelState.IsValid)
            {
                return View("Checkout", model);
            }

            ProductData.Orders.Add(new OrderConfirmationViewModel
            {
                OrderId = $"ORD-{DateTime.Now:yyyyMMdd}-{ProductData.NextOrderSeq():D4}",
                OrderStatus = "Placed",
                FullName = model.FullName,
                Email = model.Email,
                Phone = model.Phone,
                Address = model.Address,
                City = model.City,
                PostalCode = model.PostalCode,
                DeliveryOption = model.DeliveryOption,
                PaymentMethod = model.PaymentMethod,
                CreatedAt = DateTime.Now,
                EstimatedDeliveryDate = DateTime.Now.AddDays(model.DeliveryOption == "Express" ? 2 : 5),
                OrderItems = model.CartItems,
                Subtotal = model.Subtotal,
                ShippingFee = model.ShippingFee
            });

            ProductData.Cart.Clear();
            TempData["SuccessMessage"] = "Order placed successfully.";
            return RedirectToAction("ProductLanding");
        }

        private CheckoutViewModel BuildCheckoutViewModel()
        {
            var cartItems = ProductData.Cart
                .Select(ci =>
                {
                    var product = ProductData.Product.FirstOrDefault(p => p.Id == ci.ProductId);
                    if (product == null)
                    {
                        return null;
                    }

                    return new CheckoutItem
                    {
                        ProductId = product.Id,
                        Name = product.Name,
                        Image = product.Image,
                        Size = ci.Size,
                        Price = product.Price,
                        Quantity = ci.Quantity
                    };
                })
                .Where(item => item != null)
                .Cast<CheckoutItem>()
                .ToList();

            var subtotal = cartItems.Sum(item => item.Price * item.Quantity);

            return new CheckoutViewModel
            {
                CartItems = cartItems,
                Subtotal = subtotal,
                ShippingFee = ResolveShippingFee("Standard"),
                DeliveryOption = "Standard",
                PaymentMethod = "Card",
                Email = HttpContext.Session.GetString("UserEmail") ?? string.Empty,
                FullName = HttpContext.Session.GetString("DisplayName") ?? string.Empty
            };
        }

        private IActionResult RedirectToRoleHome()
        {
            if (IsSeller())
            {
                return RedirectToAction("Index", "Seller");
            }

            if (IsConsumer())
            {
                return RedirectToAction("ProductLanding");
            }

            return RedirectToAction("Login", "Account");
        }

        private bool IsSeller()
        {
            return string.Equals(HttpContext.Session.GetString("UserType"), "Seller", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsConsumer()
        {
            return string.Equals(HttpContext.Session.GetString("UserType"), "Consumer", StringComparison.OrdinalIgnoreCase);
        }

        private static decimal ResolveShippingFee(string? deliveryOption)
        {
            return string.Equals(deliveryOption, "Express", StringComparison.OrdinalIgnoreCase) ? 300m : 150m;
        }
    }
}
