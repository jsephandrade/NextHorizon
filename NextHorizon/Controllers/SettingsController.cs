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

        // 1. GET ALL SETTINGS DATA
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetDashboardSettings(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User account not found.");

            // FIX: Changed from _context.Sellers to _context.SellerAccounts
            var seller = await _context.SellerAccounts.FirstOrDefaultAsync(s => s.UserId == userId);
            
            return Ok(new 
            {
                Account = new { user.Email, user.UserType },
                Business = seller
            });
        }

        // 2. UPDATE BUSINESS PROFILE
        [HttpPut("{userId}/business")]
        public async Task<IActionResult> UpdateBusinessProfile(int userId, [FromBody] UpdateBusinessProfileDto dto)
        {
            // FIX: Changed from _context.Sellers to _context.SellerAccounts
            var seller = await _context.SellerAccounts.FirstOrDefaultAsync(s => s.UserId == userId);
            if (seller == null) return NotFound("Business profile not found.");

            seller.BusinessName = dto.BusinessName;
            seller.BusinessType = dto.BusinessType;
            seller.BusinessEmail = dto.BusinessEmail;
            seller.BusinessPhone = dto.BusinessPhone;
            seller.BusinessAddress = dto.BusinessAddress;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Business details saved successfully!" });
        }

        // 3. CHANGE PASSWORD
        [HttpPut("{userId}/password")]
        public async Task<IActionResult> ChangePassword(int userId, [FromBody] ChangePasswordDto dto)
        {
            if (dto.NewPassword != dto.ConfirmNewPassword)
                return BadRequest(new { message = "New passwords do not match." });

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound(new { message = "User not found." });

            user.PasswordHash = dto.NewPassword; 

            await _context.SaveChangesAsync();
            return Ok(new { message = "Password updated successfully!" });
        }

        // 4. UPLOAD SHOP LOGO
        [HttpPost("{userId}/logo")]
        public async Task<IActionResult> UploadLogo(int userId, IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded." });

            // FIX: Changed from _context.Sellers to _context.SellerAccounts
            var seller = await _context.SellerAccounts.FirstOrDefaultAsync(s => s.UserId == userId);
            if (seller == null) return NotFound(new { message = "Seller not found." });

            var fileExtension = Path.GetExtension(file.FileName);
            var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;

            var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "logos");
            if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
            
            var filePath = Path.Combine(uploadFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            seller.LogoPath = $"/uploads/logos/{uniqueFileName}";
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Logo uploaded successfully!", 
                logoPath = seller.LogoPath 
            });
        }
    }
}