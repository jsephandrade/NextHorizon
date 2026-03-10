// Models/PayoutAccount.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextHorizon.Models
{
    [Table("Payout_Accounts")]
    public class PayoutAccount
    {
        [Key]
        [Column("Payout_Account_Id")]
        public int PayoutAccountId { get; set; }

        [Column("User_Id")]
        public int UserId { get; set; }

        [Column("Account_Name")]
        public string? AccountName { get; set; }

        [Column("Account_Number")]
        public string? AccountNumber { get; set; }

        [Column("Card_Number")]
        public string? CardNumber { get; set; }

        [Column("Cvv_Code")]
        public string? CvvCode { get; set; }

        [Column("Expiration_Month")]
        public string? ExpirationMonth { get; set; }

        [Column("Expiration_Year")]
        public string? ExpirationYear { get; set; }

        [Column("Bank_Name")]
        public string? BankName { get; set; }

        [Column("Account_Type")]
        public string? AccountType { get; set; } // "Card" or "EWallet"

        [Column("Is_Default")]
        public bool IsDefault { get; set; }

        [Column("Postal_Code")]
        public int? PostalCode { get; set; }

        [Column("Region")]
        public string? Region { get; set; }

        [Column("Province")]
        public string? Province { get; set; }

        [Column("City")]
        public string? City { get; set; }

        [Column("Barangay")]
        public string? Barangay { get; set; }

        [Column("Street_Name")]
        public string? StreetName { get; set; }

        [Column("Building")]
        public string? Building { get; set; }

        [Column("House_No")]
        public string? HouseNo { get; set; }

        [Column("Created_At")]
        public DateTime CreatedAt { get; set; }

        [NotMapped]
        public string DisplayName
        {
            get
            {
                if (AccountType == "Card")
                {
                    var last4 = !string.IsNullOrEmpty(CardNumber) && CardNumber.Length >= 4 
                        ? CardNumber.Substring(CardNumber.Length - 4) : "";
                    return $"{BankName ?? "Card"} Ending in {last4}";
                }
                else
                {
                    var last4 = !string.IsNullOrEmpty(AccountNumber) && AccountNumber.Length >= 4 
                        ? AccountNumber.Substring(AccountNumber.Length - 4) : "";
                    return $"{BankName ?? "Wallet"} •••• {last4}";
                }
            }
        }

        [NotMapped]
        public string DisplayExpiry
        {
            get
            {
                if (AccountType == "Card" && !string.IsNullOrEmpty(ExpirationMonth) && !string.IsNullOrEmpty(ExpirationYear))
                {
                    return $"Expires {ExpirationMonth}/{ExpirationYear}";
                }
                return AccountNumber ?? "";
            }
        }
    }
}