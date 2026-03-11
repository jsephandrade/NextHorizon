using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
﻿using Microsoft.AspNetCore.Mvc;
using WebApplication1.Services;

namespace WebApplication1.ProfilePage.Controllers
{
    public class OrderController : Controller
    {
private readonly OrderService _orderService;

public OrderController(OrderService orderService)
{
    _orderService = orderService; // ✅ use DI
}

        public IActionResult MyPurchasesOptions()
        {
            // 1. Get the full list from your Service exactly like MyPurchases
            var orders = _orderService.GetUserPurchases();

            // 2. Send that list (List<OrderViewModel>) to the view
            return View(orders);
        }
    }
}