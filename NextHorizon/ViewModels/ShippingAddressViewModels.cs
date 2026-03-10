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
        public string Region { get; set; }

        [Required(ErrorMessage = "Province is required")]
        [Display(Name = "Province")]
        public string Province { get; set; }

        [Required(ErrorMessage = "City/Municipality is required")]
        [Display(Name = "City / Municipality")]
        public string CityMunicipality { get; set; }

        [Required(ErrorMessage = "Barangay is required")]
        [Display(Name = "Barangay")]
        public string Barangay { get; set; }

        [Required(ErrorMessage = "Postal code is required")]
        [Display(Name = "Postal Code")]
        [StringLength(10, MinimumLength = 4, ErrorMessage = "Postal code must be between 4 and 10 characters")]
        public string PostalCode { get; set; }

        [Required(ErrorMessage = "House/Unit/Floor number is required")]
        [Display(Name = "House No. / Unit / Floor")]
        public string HouseNumber { get; set; }

        [Display(Name = "Building (Optional)")]
        public string Building { get; set; }

        [Required(ErrorMessage = "Street name is required")]
        [Display(Name = "Street Name")]
        public string StreetName { get; set; }

        [Display(Name = "Set as default address")]
        public bool IsDefault { get; set; }

        // For displaying existing addresses
        public List<ExistingAddressItem> ExistingAddresses { get; set; } = new();
    }

    public class ExistingAddressItem
    {
        public int ShippingAddressId { get; set; }
        public string FullAddress { get; set; }
        public bool IsDefault { get; set; }
        public string Region { get; set; }
        public string Province { get; set; }
        public string CityMunicipality { get; set; }
        public string Barangay { get; set; }
        public string PostalCode { get; set; }
        public string HouseNumber { get; set; }
        public string Building { get; set; }
        public string StreetName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}