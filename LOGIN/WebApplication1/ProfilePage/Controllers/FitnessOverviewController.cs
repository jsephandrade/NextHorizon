using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
﻿using Microsoft.AspNetCore.Mvc;

namespace WebApplication1.ProfilePage.Controllers
{
    public class FitnessOverviewController : Controller
    {
        public IActionResult DailyFitnessOverview()
        {
            return View();
        }

        public IActionResult WeeklyFitnessOverview()
        {
            return View();
        }

        public IActionResult MonthlyFitnessOverview()
        {
            return View();
        }

        public IActionResult YearlyFitnessOverview()
        {
            return View();
        }
    }
}