using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
﻿using Microsoft.AspNetCore.Mvc;
using WebApplication1.Services.AdminServices;

namespace WebApplication1.ProfilePage.Controllers
{
    public class AdminController : Controller
    {
        private readonly DashboardService _dashboardService = new DashboardService();

        // URL: /Admin/Dashboard
        public IActionResult Dashboard()
        {
            var model = _dashboardService.GetHeroStats();
            return View(model);
        }

        public IActionResult Analytics()
        {
            return View();
        }

        public IActionResult Users()
        {
            return View();
        }

        public IActionResult Sellers()
        {
            return View();
        }

        public IActionResult Logistics()
        {
            return View();
        }
        public IActionResult Tasks()
        {
            return View();
        }

        public IActionResult ChallengeDetails()
        {
            return View();
        }

        public IActionResult Moderation()
        {
            return View();
        }

        public IActionResult Settings()
        {
            return View();
        }

        public IActionResult Notifications()
        {
            return View();
        }


        public IActionResult Logout()
        {

            //modify later to log out the user and redirect to login page
            return View();
        }
    }
}