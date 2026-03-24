using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextHorizon.Models
{
    public class Order
    {
        // === DATABASE FIELDS (Must match SQL exactly) ===
        
        [Key]
        public int OrderID { get; set; }

        public string? FullName { get; set; } = string.Empty; 
        public DateTime OrderDate { get; set; }

        public string? Status { get; set; } = string.Empty;
        public string? ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }

        public int seller_id { get; set; }
        public int? partner_id { get; set; }
        
       [NotMapped]
        public decimal Amount { get; set; }
        
        [NotMapped]
        public string Sku { get; set; } = string.Empty;
        
        [NotMapped]
        public string Size { get; set; } = string.Empty;

        [NotMapped]
        public string ProductImage { get; set; } = string.Empty;
        
        public string? PaymentMethod { get; set; } = string.Empty;
        
        [NotMapped]
        public string Courier { get; set; } = string.Empty;
        
        [NotMapped]
        public string TrackingNumber { get; set; } = string.Empty;
        
        [NotMapped]
        public string ReturnProofImage { get; set; } = string.Empty;
        
        [NotMapped]
        public string ReturnNote { get; set; } = string.Empty;
        public string? CancellationReason { get; set; }
        public decimal Subtotal { get; set; }
public decimal ShippingFee { get; set; }
public string? Email { get; set; }
public string? PhoneNumber { get; set; }
public string? StreetAddress { get; set; }
public string? City { get; set; }
public string? PostalCode { get; set; }
public string? DeliveryOption { get; set; }
public string? Colors { get; set; }
    }
}