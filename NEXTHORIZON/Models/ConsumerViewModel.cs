using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextHorizon.Models
{
    public class ConsumerViewModel
    {
        public int? ConsumerId { get; set; }
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Gender { get; set; }
        public string? DateJoined { get; set; }
    }
}