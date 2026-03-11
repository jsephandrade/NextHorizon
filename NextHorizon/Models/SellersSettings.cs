using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextHorizon.Models
{
    // 1. Map to dbo.Sellers
    [Table("Sellers")]
    public class Seller
    {
        [Key]
        [Column("seller_id")]
        public int Id { get; set; } // You use 'Id' in C#, but it reads 'seller_id' in SQL

        [Column("user_id")]
        public int UserId { get; set; } // Link to dbo.Users

        [Column("business_type")]
        public string BusinessType { get; set; } = string.Empty;

        [Column("business_name")]
        public string BusinessName { get; set; } = string.Empty;

        [Column("business_email")]
        public string BusinessEmail { get; set; } = string.Empty;

        [Column("business_phone")]
        public string BusinessPhone { get; set; } = string.Empty;

        [Column("business_address")]
        public string BusinessAddress { get; set; } = string.Empty;

        [Column("logo_path")]
        public string? LogoPath { get; set; }

        [Column("document_path")]
        public string? DocumentPath { get; set; }

        [Column("seller_status")]
        public string? SellerStatus { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }
    }

    // 2. Map to dbo.Users
    [Table("Users")]
    public class User
    {
        [Key]
        [Column("user_id")]
        public int Id { get; set; }

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        [Column("is_active")]
        public bool IsActive { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [Column("user_type")]
        public string? UserType { get; set; }
    }

    // 3. Map to dbo.ShippingAddresses
    [Table("ShippingAddresses")]
    public class ShippingAddress
    {
        [Key]
        [Column("shipping_address_id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; } // Link to dbo.Users

        [Column("region")]
        public string Region { get; set; } = string.Empty;

        [Column("province")]
        public string Province { get; set; } = string.Empty;

        [Column("city_municipality")]
        public string CityMunicipality { get; set; } = string.Empty;

        [Column("barangay")]
        public string Barangay { get; set; } = string.Empty;

        [Column("postal_code")]
        public string? PostalCode { get; set; }

        [Column("house_number")]
        public string HouseNumber { get; set; } = string.Empty;

        [Column("building")]
        public string? Building { get; set; }

        [Column("street_name")]
        public string StreetName { get; set; } = string.Empty;

        [Column("is_default")]
        public bool IsDefault { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}