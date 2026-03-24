using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextHorizon.Models
{
    public class AcceptOrderRequest
    {
        public int OrderId { get; set; }
        public string Courier { get; set; } = string.Empty;
    }
}