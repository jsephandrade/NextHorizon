using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyAspNetApp.Models
{
    [Table("Orders")]
    public class DbOrder
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderID { get; set; }

        public int? ConsumerID { get; set; }  // FK → Consumers.consumer_id (nullable for guest orders)

        public DateTime OrderDate { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingFee { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        public string Status { get; set; } = "Placed";

        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string StreetAddress { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string DeliveryOption { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTime EstimatedDeliveryDate { get; set; }
        public string? CancellationReason { get; set; }
    }

    [Table("OrderItems")]
    public class DbOrderItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int OrderItemID { get; set; }

        public int OrderID { get; set; }

        [Column("ProductID")]
        public int ProductId { get; set; }

        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        public string? Size { get; set; }

        public string? Color { get; set; }
    }
}
