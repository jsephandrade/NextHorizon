// ViewModels/PaymentMethodViewModel.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NextHorizon.ViewModels
{
    public class PaymentMethodViewModel
    {
        public int? PayoutAccountId { get; set; }
        public int UserId { get; set; }

        [Required]
        public string AccountType { get; set; } = "Card"; // "Card" or "EWallet"

        // Card fields
        [Display(Name = "Card Number")]
        public string CardNumber { get; set; }

        [Display(Name = "CVV")]
        public string CvvCode { get; set; }

        [Display(Name = "Expiration Month")]
        public string ExpirationMonth { get; set; }

        [Display(Name = "Expiration Year")]
        public string ExpirationYear { get; set; }

        [Display(Name = "Card Holder Name")]
        public string AccountName { get; set; }

        // E-Wallet fields
        [Display(Name = "Provider")]
        public string BankName { get; set; }

        [Display(Name = "Account Number")]
        public string AccountNumber { get; set; }

        // Billing Address
        [Required]
        public string Region { get; set; }

        [Required]
        public string Province { get; set; }

        [Required]
        public string City { get; set; }

        [Required]
        public string Barangay { get; set; }

        [Required]
        public int? PostalCode { get; set; }

        [Required]
        public string StreetName { get; set; }

        public string Building { get; set; }

        [Required]
        public string HouseNo { get; set; }

        public bool IsDefault { get; set; }

        // For displaying existing payment methods
        public List<PaymentMethodDisplayItem> ExistingMethods { get; set; } = new();
    }

    public class PaymentMethodDisplayItem
    {
        public int PayoutAccountId { get; set; }
        public string AccountType { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string DisplayExpiry { get; set; } = "";
        public bool IsDefault { get; set; }
        public string BankName { get; set; } = "";
        public string CardNumber { get; set; } = "";
        public string AccountNumber { get; set; } = "";
        public string ExpirationMonth { get; set; } = "";
        public string ExpirationYear { get; set; }  = "";
        public string AccountName { get; set; }  = "";
        public string Region { get; set; }  = "";
        public string Province { get; set; }  = "";
        public string City { get; set; }  = "";
        public string Barangay { get; set; }  = "";
        public int? PostalCode { get; set; } 
        public string StreetName { get; set; }  = "";
        public string Building { get; set; } = "";
        public string HouseNo { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}