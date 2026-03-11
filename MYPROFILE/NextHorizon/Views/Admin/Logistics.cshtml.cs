using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace NextHorizon.Views.Admin
{
    public class Logistics : PageModel
    {
        private readonly ILogger<Logistics> _logger;

        public Logistics(ILogger<Logistics> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }
    }
}