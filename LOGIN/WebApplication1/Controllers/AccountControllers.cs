using Microsoft.AspNetCore.Mvc;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.ViewModels;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Http;

namespace WebApplication1.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private const string CustomerAppUrl = "http://localhost:5296/Home/Shop";
        private const string SellerAppUrl = "http://localhost:5134";
        private const string SharedUserIdCookie = "NextHorizon.SharedUserId";
        private const string SharedUserEmailCookie = "NextHorizon.SharedUserEmail";
        private const string SharedUserTypeCookie = "NextHorizon.SharedUserType";
        private const string SharedDisplayNameCookie = "NextHorizon.SharedDisplayName";

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================
        // REGISTER GET
        // =========================
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // =========================
        // REGISTER POST
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Check if username or email already exists
            if (_context.Consumers.Any(u => u.Username == model.Username))
            {
                ModelState.AddModelError("Username", "Username already exists.");
                return View(model);
            }

            if (_context.Users.Any(u => u.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email already exists.");
                return View(model);
            }

            // Generate salt
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Hash the password
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: model.Password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            // Convert salt to base64 to store in DB
            string saltBase64 = Convert.ToBase64String(salt);

            // Create new user entity
            var user = new User
          {
        Email = model.Email,
        PasswordHash = hashed + ":" + saltBase64,
        UserType = "Consumer", // default type
        CreatedAt = DateTime.Now
    };


            // Save to database
            _context.Users.Add(user);
            _context.SaveChanges();

    // 2️⃣ Create Consumer (profile info) linked to User
    var consumer = new Consumer
    {
        UserId = user.UserId,
        Username = model.Username,
        FirstName = model.FirstName,
        MiddleName = model.MiddleName,
        LastName = model.LastName,
        Address = model.Address,
        PhoneNumber = model.PhoneNumber,
        CreatedAt = DateTime.Now
    };

    _context.Consumers.Add(consumer);
    _context.SaveChanges();

            TempData["SuccessMessage"] = "Account created successfully! You can now log in.";

            return RedirectToAction("RegisterSuccess", "Account");
        }

public IActionResult RegisterSuccess()
{
    return View();
}
 

        // =========================
        // LOGIN GET
        // =========================
      // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            var redirect = GetPostLoginRedirect(returnUrl);
            if (redirect != null)
            {
                return redirect;
            }

            return View(new LoginViewModel
            {
                ReturnUrl = returnUrl
            });
        }

    [HttpPost]
[ValidateAntiForgeryToken]
public IActionResult Login(LoginViewModel model)
{
    // 1. Check Model State (This handles "Required" and "EmailAddress" attributes)
    if (!ModelState.IsValid)
    {
        return View(model);
    }

    try 
    {
        // 2. Fetch user
        var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);
        
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }
        var consumer = _context.Consumers.FirstOrDefault(c => c.UserId == user.UserId);

        string displayName = consumer != null ? consumer.FirstName : "User";
        
        // 3. Validate Hash Format (Expected 'hash:salt')
        if (string.IsNullOrEmpty(user.PasswordHash) || !user.PasswordHash.Contains(":"))
        {
            ModelState.AddModelError(string.Empty, "Account configuration error. Please contact support.");
            return View(model);
        }

        var parts = user.PasswordHash.Split(':');
        var storedHash = parts[0];
        var storedSalt = parts[1];
        byte[] saltBytes = Convert.FromBase64String(storedSalt);

        // 4. Hash the entered password
        string enteredHash = Convert.ToBase64String(Microsoft.AspNetCore.Cryptography.KeyDerivation.KeyDerivation.Pbkdf2(
            password: model.Password,
            salt: saltBytes,
            prf: Microsoft.AspNetCore.Cryptography.KeyDerivation.KeyDerivationPrf.HMACSHA256,
            iterationCount: 10000,
            numBytesRequested: 256 / 8));

        if (enteredHash != storedHash)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        // 5. Success
        SetUserSession(user, displayName);
        SetSharedAuthCookies(user, displayName);
        TempData["SuccessMessage"] = $"Welcome, {displayName}!";
        return GetPostLoginRedirect(model.ReturnUrl) ?? RedirectToAction("ProductLanding", "Home");
    }
    catch (Exception)
    {
        ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
        return View(model);
    }
}
    
//seller

[HttpGet]
public IActionResult BecomeSeller()
{
    return View();
}

  [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> BecomeSeller(BecomeSellerViewModel model)
{
    if (!ModelState.IsValid) return View(model);

    using var transaction = _context.Database.BeginTransaction();

    try
    {
        // 1. Create the User record
        byte[] salt = new byte[16];
        using (var rng = RandomNumberGenerator.Create()) { rng.GetBytes(salt); }
        
        string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: model.Password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 10000,
            numBytesRequested: 32));

        var user = new User
        {
            Email = model.BusinessEmail,
            UserType = "Seller",
            PasswordHash = hashed + ":" + Convert.ToBase64String(salt),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync(); 

        // 2. Create the Consumer record (Personal Info)
        var consumer = new Consumer
        {
            UserId = user.UserId,
            FirstName = model.FirstName,
            MiddleName = model.MiddleName,
            LastName = model.LastName,
             Username = model.Username,
            PhoneNumber = model.PhoneNumber,
            Address = model.BusinessAddress, // Providing a value so it's not null
            CreatedAt = DateTime.Now
        };
        _context.Consumers.Add(consumer);

        // 3. File Uploads
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

        string logoPath = await SaveFile(model.BrandLogo, uploadsFolder);
        var docs = new List<string> {
            await SaveFile(model.DtiCertificate, uploadsFolder),
            await SaveFile(model.BirCertificate, uploadsFolder),
            await SaveFile(model.BusinessPermit, uploadsFolder)
        };
        if (model.AdditionalDocument != null) 
            docs.Add(await SaveFile(model.AdditionalDocument, uploadsFolder));

        // 4. Create the Seller record (Business Info)
        var seller = new Seller
        {
            UserId = user.UserId,
            BusinessType = model.BusinessType,
            BusinessName = model.BusinessName,
            BusinessEmail = model.BusinessEmail,
            BusinessPhone = model.BusinessPhone,
            TaxId = model.TaxId,
            BusinessAddress = model.BusinessAddress,
            LogoPath = logoPath,
            DocumentPath = string.Join(";", docs),
            SellerStatus = "Pending",
            CreatedAt = DateTime.Now
        };
        _context.Sellers.Add(seller);

        await _context.SaveChangesAsync();
        transaction.Commit();

        return RedirectToAction("SellerSuccess", "Account");
    }
    catch (Exception ex)
    {
        transaction.Rollback();
        // This will now show you the SPECIFIC database error in the validation summary
        var message = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
        ModelState.AddModelError("", "Database Error: " + message);
        return View(model);
    }
}

// Helper method to keep code clean
private async Task<string?> SaveFile(IFormFile file, string folder)
{
    if (file == null) return null;
    var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
    var fullPath = Path.Combine(folder, fileName);
    using (var stream = new FileStream(fullPath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }
    return "/uploads/" + fileName;
}
public IActionResult SellerSuccess()
{
    return View();
}



        // =========================
        // LOGOUT
        // =========================
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            ClearSharedAuthCookies();
            return RedirectToAction("Login", "Account");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult LogoutPost()
        {
            HttpContext.Session.Clear();
            ClearSharedAuthCookies();
            return RedirectToAction("Login", "Account");
        }

  // =========================
        // Forgot Password
        // =========================

[HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "No account found with this email.");
                return View(model);
            }

            // Generate 6-digit OTP
            var otp = new Random().Next(100000, 999999);

            // Set expiration: 10 minutes from now
            var expiresAt = DateTime.Now.AddMinutes(10);

            // Save OTP in DB
            var passwordOtp = new PasswordOtp
            {
                Email = model.Email,
                Otp = otp,
                ExpiresAt = expiresAt,
                user_id = user.UserId
            };

            _context.PasswordOtps.Add(passwordOtp);
            await _context.SaveChangesAsync();

            // Send OTP via Email (basic SMTP)
            SendOtpEmail(model.Email, otp);

            // Redirect to Verify OTP page
            return RedirectToAction("VerifyOtp", new { email = model.Email });
        }

         // =========================
        // VerifyOTP
        // =========================
 [HttpGet]
        public IActionResult VerifyOtp(string email)
        {
            var model = new VerifyOtpViewModel { Email = email };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var otpRecord = await _context.PasswordOtps
                .Where(o => o.Email == model.Email && o.Otp == model.Otp)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otpRecord == null || otpRecord.ExpiresAt < DateTime.UtcNow)
            {
                ModelState.AddModelError("", "Invalid or expired OTP.");
                return View(model);
            }

            // OTP is valid → redirect to Reset Password page
            return RedirectToAction("ResetPassword", new { email = model.Email });
        }

          // =========================
        // RESET PASSWORD
        // =========================
[HttpGet]
        public IActionResult ResetPassword(string email)
        {
            var model = new ResetPasswordViewModel {
                 Email = email,
            };
            return View(model);
        }

[HttpPost]
public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
{
    // If this is false, a hidden field is missing in your HTML
    if (!ModelState.IsValid) return View(model);

    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
    if (user == null)
    {
        ModelState.AddModelError("", "User not found.");
        return View(model);
    }

  // --- Generate new salt ---
    byte[] salt = new byte[128 / 8];
    using (var rng = RandomNumberGenerator.Create())
    {
        rng.GetBytes(salt);
    }

     // --- Hash the new password ---
    string hashed = Convert.ToBase64String(Microsoft.AspNetCore.Cryptography.KeyDerivation.KeyDerivation.Pbkdf2(
        password: model.NewPassword,
        salt: salt,
        prf: Microsoft.AspNetCore.Cryptography.KeyDerivation.KeyDerivationPrf.HMACSHA256,
        iterationCount: 10000,
        numBytesRequested: 256 / 8));

    string saltBase64 = Convert.ToBase64String(salt);

       // --- Store hash:salt in database ---
    user.PasswordHash = $"{hashed}:{saltBase64}";

    _context.Users.Update(user);
    await _context.SaveChangesAsync();

    TempData["SuccessMessage"] = "Password reset successfully!";
    return RedirectToAction("ResetPasswordSuccess", "Account");
}

[HttpGet]
public IActionResult ResetPasswordSuccess()
{
    return View();
}
        // ------------------------------
        // Helper Methods
        // ------------------------------
        private void SendOtpEmail(string email, int otp)
        {
            // Example using SmtpClient (configure properly in production)
            try
            {
                var smtp = new SmtpClient("smtp.gmail.com")
                {
                    Port = 587,
                    Credentials = new System.Net.NetworkCredential("nexthorizon398", "muir emmh zxmf vhct"),
                    EnableSsl = true
                };

                var message = new MailMessage("nexthorizon398@gmail.com", email)
                {
                    Subject = "Your OTP for Password Reset",
                    Body = $"Your OTP is: {otp}. It will expire in 10 minutes."
                };

                smtp.Send(message);
            }
            catch
            {
                // Log email sending failure
            }
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private void SetUserSession(User user, string displayName)
        {
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("UserEmail", user.Email ?? string.Empty);
            HttpContext.Session.SetString("UserType", user.UserType ?? "Consumer");
            HttpContext.Session.SetString("DisplayName", displayName);
        }

        private void SetSharedAuthCookies(User user, string displayName)
        {
            var options = new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddMinutes(30)
            };

            Response.Cookies.Append(SharedUserIdCookie, user.UserId.ToString(), options);
            Response.Cookies.Append(SharedUserEmailCookie, user.Email ?? string.Empty, options);
            Response.Cookies.Append(SharedUserTypeCookie, user.UserType ?? "Consumer", options);
            Response.Cookies.Append(SharedDisplayNameCookie, displayName ?? string.Empty, options);
        }

        private void ClearSharedAuthCookies()
        {
            Response.Cookies.Delete(SharedUserIdCookie);
            Response.Cookies.Delete(SharedUserEmailCookie);
            Response.Cookies.Delete(SharedUserTypeCookie);
            Response.Cookies.Delete(SharedDisplayNameCookie);
        }

        private IActionResult? GetPostLoginRedirect(string? returnUrl = null)
        {
            var userType = HttpContext.Session.GetString("UserType");

            if (string.Equals(userType, "Seller", StringComparison.OrdinalIgnoreCase))
            {
                return Redirect(SellerAppUrl);
            }

            if (string.Equals(userType, "Consumer", StringComparison.OrdinalIgnoreCase))
            {
                return Redirect(ResolveConsumerRedirect(returnUrl));
            }

            return null;
        }

        private static string ResolveConsumerRedirect(string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                return CustomerAppUrl;
            }

            if (Uri.TryCreate(returnUrl, UriKind.Absolute, out var absoluteUri) &&
                (absoluteUri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                 absoluteUri.Host.Equals("127.0.0.1")) &&
                (absoluteUri.Scheme == Uri.UriSchemeHttp || absoluteUri.Scheme == Uri.UriSchemeHttps))
            {
                return absoluteUri.ToString();
            }

            return CustomerAppUrl;
        }
    }
}





    

