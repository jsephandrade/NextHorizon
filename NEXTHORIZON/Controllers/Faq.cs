﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextHorizon.Models
{
    [Table("FAQs")]
    public class Faq
    {
        [Key]
        public int FaqID { get; set; }

        [Required]
        [MaxLength(500)]
        public string Question { get; set; } = string.Empty;

        [Required]
        public string Answer { get; set; } = string.Empty;  // nvarchar(max) — no MaxLength

        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = "General";

        [MaxLength(20)]
        public string Status { get; set; } = "active";

        public int? user_id { get; set; }

        [MaxLength(100)]
        public string UserType { get; set; } = "Consumer";  // "Consumer" | "Seller"

        public DateTime DateAdded { get; set; } = DateTime.Now;

        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

    
}