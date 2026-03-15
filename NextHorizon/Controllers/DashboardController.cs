using Microsoft.AspNetCore.Mvc;
using NextHorizon.Models;
using NextHorizon.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NextHorizon.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ISellerContextService _sellerContextService;
        private readonly ISellerPerformanceService _sellerPerformanceService;

        public DashboardController(
            ISellerContextService sellerContextService,
            ISellerPerformanceService sellerPerformanceService)
        {
            _sellerContextService = sellerContextService;
            _sellerPerformanceService = sellerPerformanceService;
        }

        private IActionResult? RedirectIfNotLoggedIn()
        {
            if (HttpContext.Session.GetString("SellerEmail") == null)
                return RedirectToAction("Login", "Account");
            return null;
        }

        public async Task<IActionResult> SellerDashboard(CancellationToken cancellationToken)
        {
            return RedirectIfNotLoggedIn() ?? View(await BuildSellerDashboardModelAsync(cancellationToken));
        }

        public async Task<IActionResult> OrderManagement(CancellationToken cancellationToken)
        {
            return RedirectIfNotLoggedIn() ?? View(await BuildSellerDashboardModelAsync(cancellationToken));
        }

        public IActionResult Finance()
        {
            return RedirectIfNotLoggedIn() ?? View(BuildFinanceModel());
        }

        public IActionResult TransactionHistory()
        {
            return RedirectIfNotLoggedIn() ?? View(BuildFinanceModel());
        }

        public async Task<IActionResult> OrderDetails(string id, CancellationToken cancellationToken)
        {
            if (RedirectIfNotLoggedIn() is { } redirect) return redirect;
            var dashboard = await BuildSellerDashboardModelAsync(cancellationToken);
            var order = dashboard.Orders.FirstOrDefault(o =>
                string.Equals(o.OrderId, id, StringComparison.OrdinalIgnoreCase));

            if (order is null)
            {
                return NotFound();
            }

            return View(BuildOrderDetailsModel(order, dashboard.SellerName));
        }

        private static OrderDetailsViewModel BuildOrderDetailsModel(Order order, string sellerName)
        {
            var shippingFee = order.Courier.Contains("LBC", StringComparison.OrdinalIgnoreCase) ? 120.00m : 85.00m;
            var discount = string.Equals(order.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase) ? 50.00m : 0.00m;
            var subtotal = order.TotalAmount;
            var unitPrice = order.Quantity > 0 ? decimal.Round(order.TotalAmount / order.Quantity, 2) : order.TotalAmount;

            return new OrderDetailsViewModel
            {
                SellerName = sellerName,
                OrderId = order.OrderId,
                BuyerName = order.Customer,
                OrderDateTime = order.DateTime,
                PaymentStatus = order.PaymentStatus,
                FulfillmentStatus = order.FulfillmentStatus,
                Courier = order.Courier,
                TrackingNumber = $"TRK-{order.OrderId.Replace("ORD-", string.Empty)}-{order.DateTime:MMdd}",
                ShippingAddress = "1208 Horizon Heights, Bonifacio Global City, Taguig City",
                ContactNumber = "+63 917 555 0123",
                Notes = "Leave parcel at concierge if receiver is unavailable.",
                Subtotal = subtotal,
                ShippingFee = shippingFee,
                Discount = discount,
                Items = new List<OrderLineItemViewModel>
                {
                    new OrderLineItemViewModel
                    {
                        ProductName = order.ProductName,
                        ProductImage = order.ProductImage,
                        Quantity = order.Quantity,
                        UnitPrice = unitPrice
                    }
                }
            };
        }

        private async Task<SellerDashboardViewModel> BuildSellerDashboardModelAsync(CancellationToken cancellationToken = default)
        {
            var sellerEmail = HttpContext.Session.GetString("SellerEmail");
            var sellerIdFromSession = HttpContext.Session.GetInt32("SellerId");
            var sellerNameFromSession = HttpContext.Session.GetString("SellerName");

            SellerContextInfo sellerContext;
            if (sellerIdFromSession is > 0)
            {
                sellerContext = await _sellerContextService.ResolveSellerByIdAsync(sellerIdFromSession.Value, cancellationToken);

                if (sellerContext.SellerId <= 0)
                {
                    sellerContext = new SellerContextInfo
                    {
                        SellerId = sellerIdFromSession.Value,
                        SellerName = string.IsNullOrWhiteSpace(sellerNameFromSession) ? "Seller" : sellerNameFromSession
                    };
                }
            }
            else if (!string.IsNullOrWhiteSpace(sellerEmail))
            {
                sellerContext = await _sellerContextService.ResolveSellerAsync(sellerEmail, cancellationToken);
            }
            else
            {
                sellerContext = await _sellerContextService.ResolveSellerAsync(null, cancellationToken);
            }

            if (sellerContext.SellerId > 0)
            {
                HttpContext.Session.SetInt32("SellerId", sellerContext.SellerId);
                HttpContext.Session.SetString("SellerName", string.IsNullOrWhiteSpace(sellerContext.SellerName) ? "Seller" : sellerContext.SellerName);
            }

            var performance = await _sellerPerformanceService.GetSellerPerformanceAsync(
                sellerContext.SellerId,
                DateTime.UtcNow,
                cancellationToken);
            var monthlyRevenue = await _sellerPerformanceService.GetMonthlyRevenueByYearAsync(
                sellerContext.SellerId,
                new[] { 2024, 2025, 2026 },
                cancellationToken);
            var topProducts = await _sellerPerformanceService.GetTopPerformingProductsAsync(
                sellerContext.SellerId,
                topCount: 5,
                cancellationToken: cancellationToken);

            var now = DateTime.UtcNow;
            var t1H = _sellerPerformanceService.GetTopPerformingProductsAsync(sellerContext.SellerId, topCount: 10, from: now.AddHours(-1), cancellationToken: cancellationToken);
            var t1D = _sellerPerformanceService.GetTopPerformingProductsAsync(sellerContext.SellerId, topCount: 10, from: now.AddDays(-1), cancellationToken: cancellationToken);
            var t7D = _sellerPerformanceService.GetTopPerformingProductsAsync(sellerContext.SellerId, topCount: 10, from: now.AddDays(-7), cancellationToken: cancellationToken);
            var t1M = _sellerPerformanceService.GetTopPerformingProductsAsync(sellerContext.SellerId, topCount: 10, from: now.AddDays(-30), cancellationToken: cancellationToken);
            await Task.WhenAll(t1H, t1D, t7D, t1M);

            return new SellerDashboardViewModel
            {
                SellerName = sellerContext.SellerName,
                CurrentDate = DateTime.Now,

                OrdersToShip = 14,
                PendingOrders = 5,
                LowStockAlerts = 8,
                WithdrawAmount = 15400.00m,
                WithdrawStatus = "Processing",

                TodaySales = performance.TodaySales,
                TodayUnitsSold = performance.TodayUnitsSold,
                SalesGrowth = performance.SalesGrowth,
                TotalRevenue = performance.TotalRevenue,
                TotalVisits = 423,
                MonthlyRevenueByYear = monthlyRevenue,

                RecentOrders = new List<Order>
                {
                    new Order
                    {
                        OrderId = "061231",
                        Customer = "Kiboy",
                        ProductName = "Nike Air Max 270",
                        ProductImage = "https://picsum.photos/seed/recent-1/80/80",
                        Sku = "NH-NK-001",
                        Size = "US 9",
                        DateTime = DateTime.Now.AddMinutes(-32),
                        Courier = "J&T Express",
                        Status = "Paid",
                        Amount = 5595.00m
                    },
                    new Order
                    {
                        OrderId = "061232",
                        Customer = "Loyd",
                        ProductName = "Nike Dri-FIT Training Shirt",
                        ProductImage = "https://picsum.photos/seed/recent-2/80/80",
                        Sku = "NH-NK-002",
                        Size = "L",
                        DateTime = DateTime.Now.AddHours(-1),
                        Courier = "Ninja Van",
                        Status = "To Ship",
                        Amount = 2690.00m
                    },
                    new Order
                    {
                        OrderId = "061233",
                        Customer = "Sarah",
                        ProductName = "Nike React Infinity Run FK 3",
                        ProductImage = "https://picsum.photos/seed/recent-3/80/80",
                        Sku = "NH-NK-004",
                        Size = "US 8",
                        DateTime = DateTime.Now.AddHours(-3),
                        Courier = "LBC",
                        Status = "Pending",
                        Amount = 7445.00m
                    },
                    new Order
                    {
                        OrderId = "061234",
                        Customer = "Discaya",
                        ProductName = "Nike Pro Training Shorts",
                        ProductImage = "https://picsum.photos/seed/recent-4/80/80",
                        Sku = "NH-NK-003",
                        Size = "M",
                        DateTime = DateTime.Now.AddHours(-5),
                        Courier = "J&T Express",
                        Status = "To Ship",
                        Amount = 3370.00m
                    },
                    new Order
                    {
                        OrderId = "061235",
                        Customer = "Romualdez",
                        ProductName = "Nike Sport Drawstring Bag",
                        ProductImage = "https://picsum.photos/seed/recent-5/80/80",
                        Sku = "NH-NK-005",
                        Size = "One Size",
                        DateTime = DateTime.Now.AddHours(-8),
                        Courier = "SPX Express",
                        Status = "Pending",
                        Amount = 980.00m
                    },
                    new Order
                    {
                        OrderId = "061236",
                        Customer = "Vins",
                        ProductName = "Nike Air Max 270",
                        ProductImage = "https://picsum.photos/seed/recent-6/80/80",
                        Sku = "NH-NK-001",
                        Size = "US 10",
                        DateTime = DateTime.Now.AddHours(-11),
                        Courier = "LBC",
                        Status = "Paid",
                        Amount = 11140.00m
                    },
                    new Order
                    {
                        OrderId = "061237",
                        Customer = "Kenneth",
                        ProductName = "Nike Dri-FIT Training Shirt",
                        ProductImage = "https://picsum.photos/seed/recent-7/80/80",
                        Sku = "NH-NK-002",
                        Size = "XL",
                        DateTime = DateTime.Now.AddHours(-15),
                        Courier = "Ninja Van",
                        Status = "To Ship",
                        Amount = 3970.00m
                    }
                },

                Orders = new List<Order>
                {
                    new Order
                    {
                        OrderId = "ORD-1001",
                        Customer = "Kiboy",
                        DateTime = DateTime.Now.AddHours(-4),
                        ProductImage = "https://picsum.photos/seed/order-1/80/80",
                        ProductName = "Nike Shoes",
                        Quantity = 1,
                        TotalAmount = 2650.00m,
                        PaymentStatus = "Paid",
                        FulfillmentStatus = "Pending",
                        Courier = "J&T Express"
                    },
                    new Order
                    {
                        OrderId = "ORD-1002",
                        Customer = "Loyd",
                        DateTime = DateTime.Now.AddHours(-3),
                        ProductImage = "https://picsum.photos/seed/order-2/80/80",
                        ProductName = "Croptop",
                        Quantity = 2,
                        TotalAmount = 1300.00m,
                        PaymentStatus = "COD",
                        FulfillmentStatus = "To Ship",
                        Courier = "Ninja Van"
                    },
                    new Order
                    {
                        OrderId = "ORD-1003",
                        Customer = "Sarah",
                        DateTime = DateTime.Now.AddHours(-2),
                        ProductImage = "https://picsum.photos/seed/order-3/80/80",
                        ProductName = "Tumbler",
                        Quantity = 3,
                        TotalAmount = 1950.00m,
                        PaymentStatus = "COD",
                        FulfillmentStatus = "Ship",
                        Courier = "LBC"
                    },
                    new Order
                    {
                        OrderId = "ORD-1004",
                        Customer = "Discaya",
                        DateTime = DateTime.Now.AddHours(-6),
                        ProductImage = "https://picsum.photos/seed/order-4/80/80",
                        ProductName = "Running Shorts",
                        Quantity = 1,
                        TotalAmount = 850.00m,
                        PaymentStatus = "COD",
                        FulfillmentStatus = "Delivered",
                        Courier = "LBC"
                    },
                    new Order
                    {
                        OrderId = "ORD-1005",
                        Customer = "Romualdez",
                        DateTime = DateTime.Now.AddHours(-10),
                        ProductImage = "https://picsum.photos/seed/order-5/80/80",
                        ProductName = "Sports Bottle",
                        Quantity = 1,
                        TotalAmount = 450.00m,
                        PaymentStatus = "Paid",
                        FulfillmentStatus = "Cancelled",
                        Courier = "J&T Express"
                    },
                    new Order
                    {
                        OrderId = "ORD-1006",
                        Customer = "Kenneth",
                        DateTime = DateTime.Now.AddHours(-12),
                        ProductImage = "https://picsum.photos/seed/order-6/80/80",
                        ProductName = "Training Shirt",
                        Quantity = 2,
                        TotalAmount = 1200.00m,
                        PaymentStatus = "Paid",
                        FulfillmentStatus = "Return",
                        Courier = "Ninja Van"
                    }
                },

                TopProducts = topProducts,
                TopProductsByRange = new Dictionary<string, List<TopSellingProduct>>
                {
                    ["1H"] = t1H.Result,
                    ["1D"] = t1D.Result,
                    ["7D"] = t7D.Result,
                    ["1M"] = t1M.Result,
                }
            };
        }

        private static FinanceViewModel BuildFinanceModel()
        {
            return new FinanceViewModel
            {
                SellerName = "Linda Walker",
                CurrentDate = DateTime.Now,
                AvailableBalance = 24580.00m,
                PendingPayout = 15400.00m,
                TotalWithdrawn = 78120.00m,
                TodayRevenue = 20200.00m,
                ThisMonthRevenue = 108300.00m,
                Transactions = new List<FinanceTransactionViewModel>
                {
                    new FinanceTransactionViewModel
                    {
                        ReferenceId = "TXN-9001",
                        TransactionDate = DateTime.Now.AddDays(-1),
                        Type = "Withdrawal",
                        Method = "GCash",
                        Amount = 15400.00m,
                        Status = "Processing"
                    },
                    new FinanceTransactionViewModel
                    {
                        ReferenceId = "TXN-9000",
                        TransactionDate = DateTime.Now.AddDays(-2),
                        Type = "Order Payout",
                        Method = "Platform Settlement",
                        Amount = 12650.00m,
                        Status = "Completed"
                    },
                    new FinanceTransactionViewModel
                    {
                        ReferenceId = "TXN-8998",
                        TransactionDate = DateTime.Now.AddDays(-4),
                        Type = "Refund Adjustment",
                        Method = "Wallet Adjustment",
                        Amount = -450.00m,
                        Status = "Completed"
                    },
                    new FinanceTransactionViewModel
                    {
                        ReferenceId = "TXN-8992",
                        TransactionDate = DateTime.Now.AddDays(-8),
                        Type = "Withdrawal",
                        Method = "Bank Transfer",
                        Amount = 18000.00m,
                        Status = "Completed"
                    }
                }
            };
        }

        public IActionResult MyBalance()
        {
            return RedirectIfNotLoggedIn() ?? View(BuildFinanceModel());
        }

        public IActionResult PayoutAccounts()
        {
            return RedirectIfNotLoggedIn() ?? View();
        }

        public IActionResult AddPayoutAccount()
        {
            return RedirectIfNotLoggedIn() ?? View();
        }

        public IActionResult AccountSettings()
        {
            return RedirectIfNotLoggedIn() ?? View();
        }

        public async Task<IActionResult> Analytics(CancellationToken cancellationToken)
        {
            return RedirectIfNotLoggedIn() ?? View(await BuildSellerDashboardModelAsync(cancellationToken));
        }
    }
}
