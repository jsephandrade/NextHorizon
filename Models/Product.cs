using System.ComponentModel.DataAnnotations;

namespace MyAspNetApp.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Image { get; set; } = string.Empty;
    public List<string> Images { get; set; } = new(); // Multiple product images
    public List<string> Sizes { get; set; } = new();
    public string Category { get; set; } = string.Empty; // "Men" or "Women"
    public string SubCategory { get; set; } = string.Empty; // "Running Shoes", "Apparel", "Accessories"
    public string Brand { get; set; } = string.Empty; // "Nike", "Adidas", etc.
    public List<string> AvailableColors { get; set; } = new(); // Color options
    public Dictionary<string, List<string>> ColorImages { get; set; } = new(); // color name → image gallery
    public double Rating { get; set; }
    public int ReviewCount { get; set; }
    public List<Review> Reviews { get; set; } = new();
    public int Stock { get; set; } = 50;
    public bool IsPreOrder { get; set; } = false;
    public string ExpectedReleaseDate { get; set; } = string.Empty; // e.g. "March 2026"
    public string PreOrderNote { get; set; } = string.Empty; // e.g. "Ships in 4–6 weeks"
    // Existing product sold out but available for pre-order restock
    public bool IsRestockPreOrder { get; set; } = false;
    public string RestockDate { get; set; } = string.Empty; // e.g. "Mid-March 2026"
    public string RestockNote { get; set; } = string.Empty; // e.g. "Limited restock — reserve yours now"
}

public class Review
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public double Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public bool VerifiedPurchase { get; set; } = true;
    public List<string> Images { get; set; } = new(); // Optional customer photos
    // Metric ratings
    public int? Comfort { get; set; }    // 1–5: 1=Uncomfortable, 5=Comfortable
    public int? Quality { get; set; }    // 1–5: 1=Poor, 5=Perfect
    public int? SizeFit { get; set; }    // 1=Too small, 2=Perfect, 3=Too large
    public int? WidthFit { get; set; }   // 1=Too narrow, 2=Perfect, 3=Too wide
}

public class CartItem
{
    public int ProductId { get; set; }
    public string Size { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class WishlistItem
{
    public int ProductId { get; set; }
    public DateTime AddedDate { get; set; }
}

public class PurchaseRecord
{
    public int ProductId { get; set; }
    public DateTime PurchaseDate { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public bool IsDelivered => DeliveryDate.HasValue;
}

public class CheckoutItem
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Quantity { get; set; }
}

public class CheckoutViewModel
{
    [Required(ErrorMessage = "Full name is required.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Phone number is required.")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Address is required.")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "City is required.")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Postal code is required.")]
    public string PostalCode { get; set; } = string.Empty;

    public string DeliveryOption { get; set; } = "Standard";

    public string PaymentMethod { get; set; } = "Card";
    public string? CardNumber { get; set; }
    public string? CardExpiry { get; set; }
    public string? CardCvv { get; set; }

    public List<CheckoutItem> CartItems { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal Total => Subtotal + ShippingFee;
}

public class OrderConfirmationViewModel
{
    public string OrderId { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = "Placed";
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string DeliveryOption { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = "Card";
    public DateTime CreatedAt { get; set; }
    public DateTime EstimatedDeliveryDate { get; set; }
    public List<CheckoutItem> OrderItems { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal Total => Subtotal + ShippingFee;

    public string StatusColor => OrderStatus switch
    {
        "Placed"      => "#f59e0b",
        "Processing"  => "#3b82f6",
        "Shipped"     => "#8b5cf6",
        "Delivered"   => "#22c55e",
        "Cancelled"   => "#ef4444",
        _             => "#888"
    };

    public int CurrentStep => OrderStatus switch
    {
        "Placed"      => 0,
        "Processing"  => 1,
        "Shipped"     => 2,
        "Delivered"   => 3,
        _             => 0
    };
}
