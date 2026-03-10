using Microsoft.AspNetCore.Mvc;

using NextHorizon.Models;

using System;
using System.Collections.Generic;

namespace NextHorizon.Controllers
{
    public class SellerController : Controller
    {
        // GET: /Seller/Dashboard
        public IActionResult Dashboard()
        {
            var model = new SellerDashboardViewModel
            {
                SellerName = "Seller",
                CurrentDate = DateTime.Now,
                OrdersToShip = 0,
                PendingOrders = 0,
                LowStockAlerts = 0,
                WithdrawAmount = 0m,
                WithdrawStatus = string.Empty,
                TodaySales = 0m,
                SalesGrowth = 0m,
                TotalRevenue = 0m,
                TotalVisits = 0,
                RecentOrders = new List<Order>(),
                Orders = new List<Order>(),
                TopProducts = new List<TopSellingProduct>()
            };

            return View("~/Views/Seller/SellerDashboard.cshtml", model);
        }
        public IActionResult Messenger()
        {
            return View("~/Views/Seller/SellerMessenger.cshtml");
        }
    }
}