using System;
using System.Collections.Generic;
using System.Linq;

namespace NextHorizon.Models.Admin_Models
{
    public class FinanceRequestsViewModel
    {
        public List<PayoutRequest> PendingPayouts { get; set; } = new List<PayoutRequest>();
        public List<DiscountRequest> PendingDiscounts { get; set; } = new List<DiscountRequest>();
        public List<ProductApprovalRequest> PendingProducts { get; set; } = new List<ProductApprovalRequest>();
        public List<GlobalPromotion> ActivePromotions { get; set; } = new List<GlobalPromotion>();

        public decimal TotalPendingPayoutAmount { get; set; }
        public int TotalPendingPayoutsCount { get; set; }
        public int TotalPendingDiscountsCount { get; set; }
        public int TotalPendingProductsCount { get; set; }
        public decimal TotalPendingPayoutAmountManual { get; set; }
    }

    public class PayoutRequest
    {
        public long Id { get; set; }
        public long WithdrawalId { get; set; }
        public int SellerId { get; set; }
        public int WalletId { get; set; }
        public int PayoutAccountId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime RequestedAt { get; set; }
        public DateTime DateRequested => RequestedAt;
        public DateTime? ProcessedAt { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public string ShopName { get; set; } = string.Empty;
        public string SellerEmail { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string SellerNote { get; set; } = string.Empty;
    }

    // Resolves: The type or namespace name 'ProductViewModel' could not be found
    public class ProductViewModel : ProductApprovalRequest { }

    public class DiscountRequest
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string BannerSize { get; set; } = string.Empty;
        public decimal TotalDiscountPercent { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal TotalDiscountFix { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; }
        public string ShopName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal OriginalPrice { get; set; }
        public decimal DiscountedPrice { get; set; }
        public string? ProductImage { get; set; }
        public string? MinimumRequirementType { get; set; }
        public decimal? MinimumPurchaseAmount { get; set; }
        public int? UsageLimit { get; set; }
        public bool UntilPromotionLast { get; set; }
        public int? BuyQuantity { get; set; }
        public int? TakeQuantity { get; set; }
        public string? FreeItemRequirement { get; set; }
        public int ReturnWindowDays { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsIndefinite { get; set; }
    }

    public class ProductApprovalRequest
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ShopName { get; set; } = string.Empty;
        public string SellerName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string? MainImage { get; set; }
        public DateTime SubmittedAt { get; set; }
        public string Status { get; set; } = "Pending";
        public string SubmittedAtDisplay => SubmittedAt.ToString("MMM dd, yyyy");
    }

    public class GlobalPromotion
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public byte[] BannerImage { get; set; } = Array.Empty<byte>();
        public string BannerImageName { get; set; } = string.Empty;
        public string BannerImageContentType { get; set; } = string.Empty;
        public string BannerImageBase64 { get; set; } = string.Empty;
        public decimal DiscountPercent { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsIndefinite { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; }
        public int CreatedBy { get; set; }
    }



    public class ProcessActionRequest
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty; // Fixed: Non-nullable string
        public string Reason { get; set; } = string.Empty; // Fixed: Non-nullable string
    }

    public class ProcessDiscountRequest : ProcessActionRequest { }

    public class UpdateProductStatusRequest : ProcessActionRequest { public int ProductId { get; set; }}

    public class SaveGlobalPromotionRequest
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string BannerImageBase64 { get; set; } = string.Empty;
        public string? BannerImageName { get; set; }
        public string? BannerImageContentType { get; set; }
        public decimal DiscountPercent { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsIndefinite { get; set; }
    }

    // Resolves: Non-nullable property 'ClaimCode' must contain non-null value
    public class ClaimActionRequest
    {
        public string ClaimCode { get; set; } = string.Empty;
        public string ClaimDetails { get; set; } = string.Empty;
    }
}