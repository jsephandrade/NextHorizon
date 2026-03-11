using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NextHorizon.Data;
using NextHorizon.Models;

namespace NextHorizon.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SettingsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SettingsController(AppDbContext context)
        {
            _context = context;
        }

        // 1. GET ALL SETTINGS DATA (Loads the Dashboard UI)
        // GET: api/settings/5
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetDashboardSettings(int userId)
        {
            // Fetch User Account Details
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User account not found.");

            // Fetch Business Profile
            var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
            
            // Fetch All Active Addresses
            var addresses = await _context.ShippingAddresses
                .Where(a => a.UserId == userId && a.IsActive)
                .ToListAsync();

            // Bundle it all together into one clean JSON response for React
            return Ok(new 
            {
                Account = new { user.Email, user.UserType },
                Business = seller,
                Addresses = addresses
            });
        }

        // 2. UPDATE BUSINESS PROFILE (Saves the main form)
        // PUT: api/settings/5/business
        [HttpPut("{userId}/business")]
        public async Task<IActionResult> UpdateBusinessProfile(int userId, [FromBody] UpdateBusinessProfileDto dto)
        {
            var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (seller == null) return NotFound("Business profile not found.");

            // Map the incoming React data to our SQL model
            seller.BusinessName = dto.BusinessName;
            seller.BusinessType = dto.BusinessType;
            seller.BusinessEmail = dto.BusinessEmail;
            seller.BusinessPhone = dto.BusinessPhone;
            seller.BusinessAddress = dto.BusinessAddress;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Business details saved successfully!" });
        }

        // 3. ADD NEW ADDRESS (Handles the "+ Add New Address" button)
        // POST: api/settings/5/address
        [HttpPost("{userId}/address")]
        public async Task<IActionResult> AddAddress(int userId, [FromBody] CreateAddressDto dto)
        {
            // Logic: If they check "Set as Default", we must un-default the old one
            if (dto.IsDefault)
            {
                var existingAddresses = await _context.ShippingAddresses.Where(a => a.UserId == userId).ToListAsync();
                foreach (var addr in existingAddresses)
                {
                    addr.IsDefault = false;
                }
            }

            // Create the new SQL record
            var newAddress = new ShippingAddress
            {
                UserId = userId,
                Region = dto.Region,
                Province = dto.Province,
                CityMunicipality = dto.CityMunicipality,
                Barangay = dto.Barangay,
                PostalCode = dto.PostalCode,
                HouseNumber = dto.HouseNumber,
                Building = dto.Building,
                StreetName = dto.StreetName,
                IsDefault = dto.IsDefault,
                IsActive = true, // Set to true by default when created
                CreatedAt = DateTime.UtcNow
            };

            _context.ShippingAddresses.Add(newAddress);
            await _context.SaveChangesAsync();

            return Ok(new { message = "New address added successfully!" });
        }
        // 4. DELETE ADDRESS (Triggered by the Delete button)
        // DELETE: api/settings/1/address/5
        [HttpDelete("{userId}/address/{addressId}")]
        public async Task<IActionResult> DeleteAddress(int userId, int addressId)
        {
            // We make sure to check both the addressId AND the userId for security
            var address = await _context.ShippingAddresses
                .FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId);

            if (address == null) return NotFound("Address not found.");

            // PRO TIP: In e-commerce, it is usually better to "Soft Delete" an address 
            // by setting IsActive = false, so old order history doesn't break!
            address.IsActive = false; 
            
            await _context.SaveChangesAsync();
            return Ok(new { message = "Address removed successfully!" });
        }

        // 5. SET DEFAULT ADDRESS (Triggered by the 'Set as Default' toggle)
        // PUT: api/settings/1/address/5/default
        [HttpPut("{userId}/address/{addressId}/default")]
        public async Task<IActionResult> SetDefaultAddress(int userId, int addressId)
        {
            // Find all active addresses for this user
            var addresses = await _context.ShippingAddresses
                .Where(a => a.UserId == userId && a.IsActive)
                .ToListAsync();

            // Find the specific address they want to make default
            var addressToMakeDefault = addresses.FirstOrDefault(a => a.Id == addressId);
            if (addressToMakeDefault == null) return NotFound("Address not found.");

            // Loop through all their addresses and turn off the default flag
            foreach (var addr in addresses)
            {
                addr.IsDefault = false;
            }
            
            // Turn the flag on for just the selected one
            addressToMakeDefault.IsDefault = true;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Default address updated!" });
        }
    }
}