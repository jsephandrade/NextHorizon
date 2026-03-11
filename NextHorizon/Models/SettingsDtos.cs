namespace NextHorizon.Models
{
    // Used when the seller clicks "Save" on their Business Information
    public class UpdateBusinessProfileDto
    {
        public string BusinessName { get; set; } = string.Empty;
        public string BusinessType { get; set; } = string.Empty;
        public string BusinessEmail { get; set; } = string.Empty;
        public string BusinessPhone { get; set; } = string.Empty;
        public string BusinessAddress { get; set; } = string.Empty;
    }

    // Used when the seller clicks "Add New Address"
    public class CreateAddressDto
    {
        public string Region { get; set; } = string.Empty;
        public string Province { get; set; } = string.Empty;
        public string CityMunicipality { get; set; } = string.Empty;
        public string Barangay { get; set; } = string.Empty;
        public string? PostalCode { get; set; }
        public string HouseNumber { get; set; } = string.Empty;
        public string? Building { get; set; }
        public string StreetName { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
    }
}