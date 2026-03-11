using System;
using System.Collections.Generic;
using System.Linq;
using WebApplication1.Models;

namespace WebApplication1.Data;

public static class ProductData
{
    // ── Sellers ──
    public static List<Sellers> Seller { get; } = new()
    {
        new Sellers
        {
            Id = 1,
            ShopName = "RunVault Official",
            Avatar = "https://ui-avatars.com/api/?name=RunVault&background=1a1a2e&color=fff&size=128&bold=true",
            CoverImage = "/images/cover-photo/Skinny_1920x420__35_.jpg",
            Description = "Your #1 destination for premium running shoes.",
            Location = "Manila, Philippines",
            Rating = 4.9,
            TotalRatings = 12540,
            TotalProducts = 86,
            Followers = 34200,
            ResponseRate = 98,
            ResponseTime = "within hours",
            JoinedDate = DateTime.Now.AddYears(-5)
        },

        new Sellers
        {
            Id = 2,
            ShopName = "SportZone PH",
            Avatar = "https://ui-avatars.com/api/?name=SportZone&background=0f3460&color=fff&size=128&bold=true",
            CoverImage = "/images/cover-photo/Skinny_1920x420__35_.jpg",
            Description = "Premium athletic wear & footwear.",
            Location = "Cebu City, Philippines",
            Rating = 4.7,
            TotalRatings = 8320,
            TotalProducts = 124,
            Followers = 21500,
            ResponseRate = 95,
            ResponseTime = "within minutes",
            JoinedDate = DateTime.Now.AddYears(-3)
        }
    };

    // ── Products ──
    public static List<Product> Product { get; } = new()
    {
        new Product
        {
            Id = 1,
            Name = "Horizon Elite Road Runner",
            Description = "Premium carbon-plated running shoes engineered for speed and endurance.",
            Price = 8999.00m,
            Image = "/images/Lime Shimmer-Green Lux/PUMA-x-ASTON-MARTIN-ARAMCO-F1®-TEAM-Fade-Men's-Sneakers.avif",
            Sizes = new List<string> { "US 7", "US 8", "US 9", "US 10", "US 11" },
            Category = "Men",
            SubCategory = "Running Shoes",
            Brand = "Nike",
            AvailableColors = new List<string> { "Green", "Black" },
            Rating = 4.8,
            ReviewCount = 245,
            Reviews = new List<Review>()
        },

        new Product
        {
            Id = 2,
            Name = "Sprint Pro Training Shoes",
            Description = "Versatile training shoes with responsive cushioning.",
            Price = 6499.00m,
            Image = "https://images.pexels.com/photos/1598505/pexels-photo-1598505.jpeg",
            Sizes = new List<string> { "US 7", "US 8", "US 9", "US 10" },
            Category = "Men",
            SubCategory = "Running Shoes",
            Brand = "Adidas",
            SellerId = 2,
            AvailableColors = new List<string> { "White", "Grey", "Black" },
            Rating = 4.6,
            ReviewCount = 198,
            Reviews = new List<Review>()
        }
    };

    // ── Cart ──
    public static List<CartItem> Cart { get; } = new();

    // ── Wishlist ──
    public static List<WishlistItem> Wishlist { get; } = new();

    // ── Order Generator ──
    private static int _orderSeq = 3;
    public static int NextOrderSeq() => ++_orderSeq;

    // ── Orders ──
    public static List<OrderConfirmationViewModel> Orders { get; } = new()
    {
        new OrderConfirmationViewModel
        {
            OrderId = "ORD-20260215-0001",
            OrderStatus = "Delivered",
            FullName = "Juan Dela Cruz",
            Email = "juan@nexthorizon.ph",
            Phone = "+63 912 345 6789",
            Address = "123 Rizal St",
            City = "Manila",
            PostalCode = "1000",
            DeliveryOption = "Standard",
            PaymentMethod = "Card",
            CreatedAt = DateTime.Now.AddDays(-15),
            EstimatedDeliveryDate = DateTime.Now.AddDays(-10),
            Subtotal = 13335m,
            ShippingFee = 150m,
            OrderItems = new List<CheckoutItem>
            {
                new CheckoutItem
                {
                    ProductId = 1,
                    Name = "Horizon Elite Road Runner",
                    Image = "/images/Lime Shimmer-Green Lux/PUMA-x-ASTON-MARTIN-ARAMCO-F1®-TEAM-Fade-Men's-Sneakers.avif",
                    Size = "US 9",
                    Price = 8999m,
                    Quantity = 1
                }
            }
        }
    };

    // ── Purchase Records ──
    public static List<PurchaseRecord> PurchaseRecords { get; } = new()
    {
        new PurchaseRecord
        {
            ProductId = 1,
            PurchaseDate = DateTime.Now.AddDays(-30),
            DeliveryDate = DateTime.Now.AddDays(-20)
        }
    };
}