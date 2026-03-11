using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NextHorizon.ViewModels
{
    public class ShippingAddressViewModel
    {
        public int UserId { get; set; }
        
        public int? ShippingAddressId { get; set; }

        [Required(ErrorMessage = "Region is required")]
        [Display(Name = "Region")]
        public string Region { get; set; } = string.Empty;

        [Required(ErrorMessage = "Province is required")]
        [Display(Name = "Province")]
        public string Province { get; set; } = string.Empty;

        [Required(ErrorMessage = "City/Municipality is required")]
        [Display(Name = "City / Municipality")]
        public string CityMunicipality { get; set; } = string.Empty;

        [Required(ErrorMessage = "Barangay is required")]
        [Display(Name = "Barangay")]
        public string Barangay { get; set; } = string.Empty;

        [Required(ErrorMessage = "Postal code is required")]
        [Display(Name = "Postal Code")]
        [StringLength(10, MinimumLength = 4, ErrorMessage = "Postal code must be between 4 and 10 characters")]
        public string PostalCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "House/Unit/Floor number is required")]
        [Display(Name = "House No. / Unit / Floor")]
        public string HouseNumber { get; set; } = string.Empty;

        [Display(Name = "Building (Optional)")]
        public string? Building { get; set; }

        [Required(ErrorMessage = "Street name is required")]
        [Display(Name = "Street Name")]
        public string StreetName { get; set; } = string.Empty;

        [Display(Name = "Set as default address")]
        public bool IsDefault { get; set; }

        // For displaying existing addresses
        public List<ExistingAddressItem> ExistingAddresses { get; set; } = new();
    }

    public class ExistingAddressItem
    {
        public int ShippingAddressId { get; set; }
        public string FullAddress { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public string Region { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string CityMunicipality { get; set; } = string.Empty;
        public string Barangay { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string HouseNumber { get; set; } = string.Empty;
        public string Building { get; set; } = string.Empty;
        public string StreetName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
