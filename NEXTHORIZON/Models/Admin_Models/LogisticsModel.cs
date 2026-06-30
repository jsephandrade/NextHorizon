using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NextHorizon.Models.Admin_Models
{
    // Logistics Models
    public class LogisticsPartner
    {
        public int LogisticsId { get; set; }
        
        [Required(ErrorMessage = "Courier name is required")]
        [StringLength(100, ErrorMessage = "Courier name cannot exceed 100 characters")]
        public string CourierName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Service type is required")]
        [StringLength(50)]
        public string ServiceType { get; set; } = string.Empty;
        
        [Url(ErrorMessage = "Invalid URL format")]
        public string LogoUrl { get; set; } = string.Empty;
        public string LogoBase64 { get; set; } = string.Empty;
        public string LogoFilename { get; set; } = string.Empty;
        public string LogoContentType { get; set; } = string.Empty;
        
        [RegularExpression("^(Active|Inactive|Archived)$", ErrorMessage = "Status must be Active, Inactive, or Archived")]
        public string Status { get; set; } = "Active";
        
        [Range(0, 100, ErrorMessage = "Success rate must be between 0 and 100")]
        public decimal SuccessRate { get; set; }
        
        [Range(0, 30, ErrorMessage = "Average delivery days must be between 0 and 30")]
        public decimal AvgDeliveryDays { get; set; }
        
        [Range(1, 30, ErrorMessage = "Min delivery days must be between 1 and 30")]
        public int? MinDeliveryDays { get; set; }
        
        [Range(1, 30, ErrorMessage = "Max delivery days must be between 1 and 30")]
        public int? MaxDeliveryDays { get; set; }
        
        [StringLength(100)]
        public string ContactPerson { get; set; } = string.Empty;
        
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(100)]
        public string ContactEmail { get; set; } = string.Empty;
        
        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(20)]
        public string ContactPhone { get; set; } = string.Empty;
        
        [Url(ErrorMessage = "Invalid URL format")]
        [StringLength(500)]
        public string TrackingUrlTemplate { get; set; } = string.Empty;
        
        public bool IsPreferred { get; set; }
        public int SortOrder { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string DisplayStatus { get; set; } = string.Empty;
        
        // Performance metrics calculated from orders
        public int? TotalOrders { get; set; }
        public int? DeliveredOrders { get; set; }
        public int? Last30DaysOrders { get; set; }
        
        // Additional calculated properties
        public string DeliveryRange => MinDeliveryDays.HasValue && MaxDeliveryDays.HasValue 
            ? $"{MinDeliveryDays}-{MaxDeliveryDays} Days" 
            : $"{AvgDeliveryDays:F1} Days";
            
        public string SuccessRateDisplay => $"{SuccessRate:F1}%";
    }

    public class SaveLogisticsRequest
    {
        public int? LogisticsId { get; set; }
        
        [Required(ErrorMessage = "Courier name is required")]
        [StringLength(100)]
        public string CourierName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Service type is required")]
        [StringLength(50)]
        public string ServiceType { get; set; } = string.Empty;
        
        public string LogoBase64 { get; set; } = string.Empty;
        public string LogoFilename { get; set; } = string.Empty;
        public string LogoContentType { get; set; } = string.Empty;
        
        [RegularExpression("^(Active|Inactive|Archived)$")]
        public string Status { get; set; } = "Active";
        
        [StringLength(100)]
        public string ContactPerson { get; set; } = string.Empty;
        
        [EmailAddress]
        [StringLength(100)]
        public string ContactEmail { get; set; } = string.Empty;
        
        [Phone]
        [StringLength(20)]
        public string ContactPhone { get; set; } = string.Empty;
        
        [Url]
        [StringLength(500)]
        public string TrackingUrlTemplate { get; set; } = string.Empty;
        
        public bool IsPreferred { get; set; }
        public int SortOrder { get; set; }
    }

    public class UpdateLogisticsStatusRequest
    {
        [Required]
        public int LogisticsId { get; set; }
        
        [Required]
        [RegularExpression("^(Active|Inactive|Archived)$")]
        public string Status { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Reason is required for status change")]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;
    }

    public class DeleteLogisticsRequest
    {
        [Required]
        public int LogisticsId { get; set; }
        
        [Required(ErrorMessage = "Reason is required for deletion")]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;
    }

    public class RestoreLogisticsRequest
    {
        [Required]
        public int LogisticsId { get; set; }
        
        [Required(ErrorMessage = "Reason is required for restoration")]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;
    }

    public class LogisticsStats
    {
        public int TotalCount { get; set; }
        public int ActiveCount { get; set; }
        public int InactiveCount { get; set; }
        public int ArchivedCount { get; set; }
        
        // Additional useful stats
        public decimal AverageSuccessRate { get; set; }
        public decimal AverageDeliveryDays { get; set; }
        public int PreferredPartnersCount { get; set; }
    }

    public class LogisticsPerformanceDetails
    {
        public int LogisticsId { get; set; }
        public string CourierName { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int FailedOrders { get; set; }
        public int CancelledOrders { get; set; }
        public int InTransitOrders { get; set; }
        public int PendingOrders { get; set; }
        public decimal SuccessRate { get; set; }
        public decimal AvgDeliveryDays { get; set; }
        public decimal? MinDeliveryDays { get; set; }
        public decimal? MaxDeliveryDays { get; set; }
        public int Last7DaysOrders { get; set; }
        public int Last30DaysOrders { get; set; }
        public int Last90DaysOrders { get; set; }
        public int? ConfiguredMinDays { get; set; }
        public int? ConfiguredMaxDays { get; set; }
    }

    public class MonthlyPerformance
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public decimal SuccessRate { get; set; }
        public decimal AvgDeliveryDays { get; set; }
    }
}
