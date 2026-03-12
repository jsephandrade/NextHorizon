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
            
            // Bundle it all together into one clean JSON response for React
            return Ok(new 
            {
                Account = new { user.Email, user.UserType },
                Business = seller
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
            //seller.TaxId = dto.TaxId;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Business details saved successfully!" });
        }
        // 3. CHANGE PASSWORD
        // PUT: api/settings/5/password
        [HttpPut("{userId}/password")]
        public async Task<IActionResult> ChangePassword(int userId, [FromBody] ChangePasswordDto dto)
        {
            // 1. Basic validation
            if (dto.NewPassword != dto.ConfirmNewPassword)
                return BadRequest(new { message = "New passwords do not match." });

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound(new { message = "User not found." });

            // 2. IMPORTANT: In a production app, you MUST verify the hash of the CurrentPassword here!
            // Example: if (!VerifyPasswordHash(dto.CurrentPassword, user.PasswordHash)) return BadRequest("Incorrect current password.");
            
            // 3. Update the password
            // In a real app, hash this before saving: user.PasswordHash = HashPassword(dto.NewPassword);
            user.PasswordHash = dto.NewPassword; 

            await _context.SaveChangesAsync();
            return Ok(new { message = "Password updated successfully!" });
        }
        // 4. UPLOAD SHOP LOGO
        // POST: api/settings/5/logo
        [HttpPost("{userId}/logo")]
        public async Task<IActionResult> UploadLogo(int userId, IFormFile file)
        {
            // 1. Check if they actually sent a file
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "No file uploaded." });

            var seller = await _context.Sellers.FirstOrDefaultAsync(s => s.UserId == userId);
            if (seller == null) return NotFound(new { message = "Seller not found." });

            // 2. Generate a unique file name (so 'logo.png' doesn't overwrite someone else's 'logo.png')
            var fileExtension = Path.GetExtension(file.FileName);
            var uniqueFileName = Guid.NewGuid().ToString() + fileExtension;

            // 3. Define where to save it (e.g., inside the wwwroot/uploads/logos folder)
            var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "logos");
            if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
            
            var filePath = Path.Combine(uploadFolder, uniqueFileName);

            // 4. Save the physical file to your server
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 5. Save the text path into your SQL Server database
            seller.LogoPath = $"/uploads/logos/{uniqueFileName}";
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Logo uploaded successfully!", 
                logoPath = seller.LogoPath 
            });
        }
    }
}