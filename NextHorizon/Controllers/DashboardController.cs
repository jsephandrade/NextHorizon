using Microsoft.AspNetCore.Mvc;
using NextHorizon.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NextHorizon.Controllers
{
    public class DashboardController : Controller
    {
        public IActionResult SellerDashboard()
        {
            return View(BuildSellerDashboardModel());
        }

        public IActionResult OrderManagement()
        {
            return View(BuildSellerDashboardModel());
        }

        public IActionResult Finance()
        {
            return View(BuildFinanceModel());
        }

        public IActionResult TransactionHistory()
        {
            return View(BuildFinanceModel());
        }

        public IActionResult OrderDetails(string id)
        {
            var dashboard = BuildSellerDashboardModel();
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

        private static SellerDashboardViewModel BuildSellerDashboardModel()
        {
            return new SellerDashboardViewModel
            {
                SellerName = "Linda Walker",
                CurrentDate = DateTime.Now,

                OrdersToShip = 14,
                PendingOrders = 5,
                LowStockAlerts = 8,
                WithdrawAmount = 15400.00m,
                WithdrawStatus = "Processing",

                TodaySales = 20200.00m,
                SalesGrowth = 8.06m,
                TotalRevenue = 108300.00m,
                TotalVisits = 423,

                RecentOrders = new List<Order>
                {
                    new Order { OrderId = "061231", Customer = "Kiboy", Status = "Paid", Amount = 650.00m },
                    new Order { OrderId = "061232", Customer = "Loyd", Status = "To Ship", Amount = 650.00m },
                    new Order { OrderId = "061233", Customer = "Sarah", Status = "Pending", Amount = 650.00m },
                    new Order { OrderId = "061234", Customer = "Discaya", Status = "To Ship", Amount = 650.00m },
                    new Order { OrderId = "061235", Customer = "Romualdez", Status = "Pending", Amount = 650.00m },
                    new Order { OrderId = "061236", Customer = "Vins", Status = "Paid", Amount = 650.00m },
                    new Order { OrderId = "061237", Customer = "Kenneth", Status = "To Ship", Amount = 650.00m }
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

                TopProducts = new List<TopSellingProduct>
                {
                    new TopSellingProduct { ProductName = "Nike Shoes", ImageUrl = "https://picsum.photos/seed/nike-shoes/120/120", UnitsSold = 16, Rank = 1 },
                    new TopSellingProduct { ProductName = "Croptop", ImageUrl = "https://picsum.photos/seed/croptop/120/120", UnitsSold = 12, Rank = 2 },
                    new TopSellingProduct { ProductName = "Tumbler", ImageUrl = "https://picsum.photos/seed/tumbler/120/120", UnitsSold = 6, Rank = 3 },
                    new TopSellingProduct { ProductName = "3 Men's Sleeveless", ImageUrl = "https://picsum.photos/seed/sleeveless/120/120", UnitsSold = 4, Rank = 4 }
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
            return View(BuildFinanceModel());
        }

        public IActionResult PayoutAccounts()
        {
            return View();
        }

        public IActionResult AddPayoutAccount()
        {
            return View();
        }

        public IActionResult Analytics()
        {
            var model = new SellerDashboardViewModel();

            model.SellerName = "Seller"; // or get from database

            return View(model);
        }
    }
}
