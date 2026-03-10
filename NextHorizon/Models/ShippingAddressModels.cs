using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NextHorizon.Models
{
        
    [Table("ShippingAddresses")]
    public class ShippingAddress
    {
        [Key]
        [Column("shipping_address_id")]
        public int ShippingAddressId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("region")]
        public string Region { get; set; } = ""; // Default to empty string

        [Column("province")]
        public string Province { get; set; } = "";

        [Column("city_municipality")]
        public string CityMunicipality { get; set; } = "";

        [Column("barangay")]
        public string Barangay { get; set; } = "";

        [Column("postal_code")]
        public string PostalCode { get; set; } = "";

        [Column("house_number")]
        public string HouseNumber { get; set; } = "";

        [Column("building")]
        public string? Building { get; set; } // Keep nullable

        [Column("street_name")]
        public string StreetName { get; set; } = "";

        [Column("is_default")]
        public bool IsDefault { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

         // Navigation property
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [NotMapped]
        public string FullAddress
        {
            get
            {
                var address = $"{HouseNumber} {StreetName}";
                if (!string.IsNullOrWhiteSpace(Building))
                    address += $", {Building}";
                address += $", {Barangay}, {CityMunicipality}, {Province}, {Region} {PostalCode}";
                return address;
            }
        }
    }
}