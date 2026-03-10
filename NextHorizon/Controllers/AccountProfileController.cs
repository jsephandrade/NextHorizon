using Microsoft.AspNetCore.Mvc;
using NextHorizon.Services;
using NextHorizon.Models;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using NextHorizon.Models.Dtos;
using Microsoft.EntityFrameworkCore;
using NextHorizon.ViewModels;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;

namespace NextHorizon.Controllers
{
    public class AccountProfileController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly OrderService _orderService;
        public AccountProfileController(ApplicationDbContext db, OrderService orderService)
        {
            _db = db;
            _orderService = orderService;
        }

        public async Task<IActionResult> ProfileView()
        {
            var userId = HttpContext.Session.GetInt32("UserId");    

            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "AccountProfile");
            }

            int uid = userId.Value;
            var today = DateTime.Today;

            // Header (username, quote, rank)
            var headerList = await _db.ProfileHeaderRows
                .FromSqlRaw("EXEC dbo.sp_Profile_Header @UserId={0}", uid)
                .AsNoTracking()
                .ToListAsync();

            var header = headerList.FirstOrDefault() ?? new ProfileHeaderRow();

            // Today stats (distance, time, count)
            var todayStatsList = await _db.ProfileTodayStatsRows
                .FromSqlRaw("EXEC dbo.sp_Profile_TodayStats @UserId={0}, @Day={1}", uid, today)
                .AsNoTracking()
                .ToListAsync();

            var todayStats = todayStatsList.FirstOrDefault() ?? new ProfileTodayStatsRow();

            // Recent uploads list
            var recent = await _db.ProfileRecentUploadRows
                .FromSqlRaw("EXEC dbo.sp_Profile_RecentUploads @UserId={0}, @TopN={1}", uid, 6)
                .AsNoTracking()
                .ToListAsync();

            var vm = new ProfileViewVM
            {
                Header = header,
                Today = todayStats,
                RecentUploads = recent
            };

            return View(vm);
        }
                public IActionResult MyPurchases()
        {
            // Get the list from your new Service
            var orders = _orderService.GetUserPurchases();

            // Send that list to the MyPurchases.cshtml view
            return View(orders);
        }
        [HttpGet]
        public async Task<IActionResult> UpdateProfile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login");
            }

            var user = await _db.Users
                .Include(u => u.Consumer)
                .FirstOrDefaultAsync(u => u.UserId == userId.Value);

            if (user == null)
            {
                return NotFound();
            }

            // If consumer doesn't exist, create one using stored procedure
            if (user.Consumer == null)
            {
                var parameters = new[]
                {
                    new SqlParameter("@UserId", userId.Value)
                };
                
                await _db.Database.ExecuteSqlRawAsync("EXEC dbo.sp_Profile_CreateConsumer @UserId", parameters);
                
                // Reload user with consumer
                user = await _db.Users
                    .Include(u => u.Consumer)
                    .FirstOrDefaultAsync(u => u.UserId == userId.Value);
            }

            var viewModel = new UpdateProfileViewModel
            {
                UserId = user.UserId,
                FirstName = user.Consumer?.FirstName,
                MiddleName = user.Consumer?.MiddleName,
                LastName = user.Consumer?.LastName,
                FullName = user.Consumer?.FullName,
                Username = user.Consumer?.Username,
                Address = user.Consumer?.Address,
                PhoneNumber = user.Consumer?.PhoneNumber,
                Email = user.Email
            };

            return View(viewModel);
        }

        public IActionResult UploadActivity()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ShippingAddress()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "AccountProfile");
            }

            var viewModel = new ShippingAddressViewModel
            {
                UserId = userId.Value,
                ExistingAddresses = new List<ExistingAddressItem>()
            };

            try
            {
                // Get existing addresses using stored procedure
                var addresses = await _db.ShippingAddresses
                    .FromSqlRaw("EXEC dbo.sp_ShippingAddress_GetByUser @UserId={0}", userId.Value)
                    .AsNoTracking()
                    .ToListAsync();

                // Map to ViewModel with null handling
                viewModel.ExistingAddresses = addresses.Select(a => new ExistingAddressItem
                {
                    ShippingAddressId = a.ShippingAddressId,
                    FullAddress = a.FullAddress ?? "",
                    IsDefault = a.IsDefault,
                    Region = a.Region ?? "",
                    Province = a.Province ?? "",
                    CityMunicipality = a.CityMunicipality ?? "",
                    Barangay = a.Barangay ?? "",
                    PostalCode = a.PostalCode ?? "",
                    HouseNumber = a.HouseNumber ?? "",
                    Building = a.Building ?? "",
                    StreetName = a.StreetName ?? "",
                    CreatedAt = a.CreatedAt
                }).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading addresses: {ex.Message}");
                // Return empty list on error
            }

            return View(viewModel);
        }

       [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveShippingAddress(ShippingAddressViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "Not logged in" });
            }

            try
            {
                model.UserId = userId.Value;

                // Ensure null values are handled
                var parameters = new[]
                {
                    new SqlParameter("@UserId", userId.Value),
                    new SqlParameter("@Region", model.Region ?? ""),
                    new SqlParameter("@Province", model.Province ?? ""),
                    new SqlParameter("@CityMunicipality", model.CityMunicipality ?? ""),
                    new SqlParameter("@Barangay", model.Barangay ?? ""),
                    new SqlParameter("@PostalCode", model.PostalCode ?? ""),
                    new SqlParameter("@HouseNumber", model.HouseNumber ?? ""),
                    new SqlParameter("@Building", string.IsNullOrEmpty(model.Building) ? DBNull.Value : (object)model.Building),
                    new SqlParameter("@StreetName", model.StreetName ?? ""),
                    new SqlParameter("@IsDefault", model.IsDefault)
                };

                if (model.ShippingAddressId.HasValue && model.ShippingAddressId > 0)
                {
                    // Add ID parameter for update
                    var updateParams = new List<SqlParameter>
                    {
                        new SqlParameter("@ShippingAddressId", model.ShippingAddressId)
                    };
                    updateParams.AddRange(parameters);
                    
                    await _db.Database.ExecuteSqlRawAsync(
                        "EXEC dbo.sp_ShippingAddress_Update @ShippingAddressId, @UserId, @Region, @Province, @CityMunicipality, @Barangay, @PostalCode, @HouseNumber, @Building, @StreetName, @IsDefault",
                        updateParams.ToArray());

                    return Json(new { success = true, message = "Address updated successfully!" });
                }
                else
                {
                    await _db.Database.ExecuteSqlRawAsync(
                        "EXEC dbo.sp_ShippingAddress_Insert @UserId, @Region, @Province, @CityMunicipality, @Barangay, @PostalCode, @HouseNumber, @Building, @StreetName, @IsDefault",
                        parameters);

                    return Json(new { success = true, message = "Address added successfully!" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateShippingAddress(ShippingAddressViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue || model.ShippingAddressId == null)
            {
                return RedirectToAction("Login", "AccountProfile");
            }

            try
            {
                var parameters = new[]
                {
                    new SqlParameter("@ShippingAddressId", model.ShippingAddressId),
                    new SqlParameter("@UserId", userId.Value),
                    new SqlParameter("@Region", model.Region),
                    new SqlParameter("@Province", model.Province),
                    new SqlParameter("@CityMunicipality", model.CityMunicipality),
                    new SqlParameter("@Barangay", model.Barangay),
                    new SqlParameter("@PostalCode", model.PostalCode),
                    new SqlParameter("@HouseNumber", model.HouseNumber),
                    new SqlParameter("@Building", model.Building ?? (object)DBNull.Value),
                    new SqlParameter("@StreetName", model.StreetName),
                    new SqlParameter("@IsDefault", model.IsDefault)
                };

                await _db.Database.ExecuteSqlRawAsync(
                    "EXEC dbo.sp_ShippingAddress_Update @ShippingAddressId, @UserId, @Region, @Province, @CityMunicipality, @Barangay, @PostalCode, @HouseNumber, @Building, @StreetName, @IsDefault", 
                    parameters);

                TempData["SuccessMessage"] = "Address updated successfully!";
                return RedirectToAction("ShippingAddress");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error updating address: {ex.Message}");
                return View("ShippingAddress", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteShippingAddress(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "Not logged in" });
            }

            try
            {
                var parameters = new[]
                {
                    new SqlParameter("@ShippingAddressId", id),
                    new SqlParameter("@UserId", userId.Value)
                };

                await _db.Database.ExecuteSqlRawAsync(
                    "EXEC dbo.sp_ShippingAddress_Delete @ShippingAddressId, @UserId",
                    parameters);

                return Json(new { success = true, message = "Address deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefaultShippingAddress(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "Not logged in" });
            }

            try
            {
                var parameters = new[]
                {
                    new SqlParameter("@ShippingAddressId", id),
                    new SqlParameter("@UserId", userId.Value)
                };

                await _db.Database.ExecuteSqlRawAsync(
                    "EXEC dbo.sp_ShippingAddress_SetDefault @ShippingAddressId, @UserId",
                    parameters);

                return Json(new { success = true, message = "Default address updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> PaymentMethods()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "AccountProfile");
            }

            var viewModel = new PaymentMethodViewModel
            {
                UserId = userId.Value,
                ExistingMethods = new List<PaymentMethodDisplayItem>()
            };

            try
            {
                Console.WriteLine($"Loading payment methods for user: {userId.Value}");
                
                // Use direct query instead of stored procedure
                var methods = await _db.PayoutAccounts
                    .Where(p => p.UserId == userId.Value)
                    .OrderByDescending(p => p.IsDefault)
                    .ThenByDescending(p => p.CreatedAt)
                    .ToListAsync();

                Console.WriteLine($"Found {methods.Count} records in database");

                if (methods.Any())
                {
                    viewModel.ExistingMethods = methods.Select(m => 
                    {
                        // Safely handle null values
                        string accountType = m.AccountType ?? "";
                        string bankName = m.BankName ?? "";
                        string cardNumber = m.CardNumber ?? "";
                        string accountNumber = m.AccountNumber ?? "";
                        string expirationMonth = m.ExpirationMonth ?? "";
                        string expirationYear = m.ExpirationYear ?? "";
                        string accountName = m.AccountName ?? "";
                        string region = m.Region ?? "";
                        string province = m.Province ?? "";
                        string city = m.City ?? "";
                        string barangay = m.Barangay ?? "";
                        string streetName = m.StreetName ?? "";
                        string building = m.Building ?? "";
                        string houseNo = m.HouseNo ?? "";

                        // Safely get last 4 digits
                        string last4 = "****";
                        if (accountType == "Card" && !string.IsNullOrEmpty(cardNumber) && cardNumber.Length >= 4)
                        {
                            last4 = cardNumber.Substring(cardNumber.Length - 4);
                        }
                        else if (!string.IsNullOrEmpty(accountNumber) && accountNumber.Length >= 4)
                        {
                            last4 = accountNumber.Substring(accountNumber.Length - 4);
                        }

                        string displayName = accountType == "Card" 
                            ? $"{(string.IsNullOrEmpty(bankName) ? "Card" : bankName)} Ending in {last4}"
                            : (string.IsNullOrEmpty(bankName) ? "E-Wallet" : bankName);

                        string displayExpiry = accountType == "Card" 
                            ? $"Expires {expirationMonth}/{expirationYear}"
                            : accountNumber;

                        return new PaymentMethodDisplayItem
                        {
                            PayoutAccountId = m.PayoutAccountId,
                            AccountType = accountType,
                            DisplayName = displayName,
                            DisplayExpiry = displayExpiry,
                            IsDefault = m.IsDefault,
                            BankName = bankName,
                            CardNumber = cardNumber,
                            AccountNumber = accountNumber,
                            ExpirationMonth = expirationMonth,
                            ExpirationYear = expirationYear,
                            AccountName = accountName,
                            Region = region,
                            Province = province,
                            City = city,
                            Barangay = barangay,
                            PostalCode = m.PostalCode,
                            StreetName = streetName,
                            Building = building,
                            HouseNo = houseNo,
                            CreatedAt = m.CreatedAt
                        };
                    }).ToList();
                    
                    Console.WriteLine($"Mapped {viewModel.ExistingMethods.Count} items to ViewModel");
                }
                else
                {
                    Console.WriteLine("No payment methods found in database");
                    
                    // Check total records
                    var totalCount = await _db.PayoutAccounts.CountAsync();
                    Console.WriteLine($"Total records in Payout_Accounts table: {totalCount}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading payment methods: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            return View(viewModel);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
       public async Task<IActionResult> SavePaymentMethod()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "Not logged in" });
            }

            try
            {
                // Read the request body as string
                string jsonString;
                using (var reader = new StreamReader(Request.Body))
                {
                    jsonString = await reader.ReadToEndAsync();
                }

                Console.WriteLine("Received JSON: " + jsonString);

                // Parse as JsonDocument
                using JsonDocument document = JsonDocument.Parse(jsonString);
                JsonElement root = document.RootElement;

                // Parse PayoutAccountId (nullable)
                int? payoutAccountId = null;
                if (root.TryGetProperty("PayoutAccountId", out var payoutIdElement) && 
                    payoutIdElement.ValueKind != JsonValueKind.Null)
                {
                    if (payoutIdElement.ValueKind == JsonValueKind.Number)
                    {
                        payoutAccountId = payoutIdElement.GetInt32();
                    }
                    else
                    {
                        var idStr = payoutIdElement.GetString();
                        if (!string.IsNullOrEmpty(idStr) && idStr != "null" && int.TryParse(idStr, out int id))
                        {
                            payoutAccountId = id;
                        }
                    }
                }

                // Get AccountType first to determine which fields to validate
                string accountType = root.TryGetProperty("AccountType", out var accountTypeElement) 
                    ? accountTypeElement.GetString() ?? "" 
                    : "";

                // Parse the rest of the model with conditional null handling
                var model = new PaymentMethodViewModel
                {
                    UserId = userId.Value,
                    PayoutAccountId = payoutAccountId,
                    AccountType = accountType,
                    AccountName = root.TryGetProperty("AccountName", out var accountNameElement) ? accountNameElement.GetString() : "",
                    
                    // Card fields - only parse if AccountType is Card
                    CardNumber = (accountType == "Card" && root.TryGetProperty("CardNumber", out var cardNumberElement)) 
                        ? cardNumberElement.GetString() : null,
                    CvvCode = (accountType == "Card" && root.TryGetProperty("CvvCode", out var cvvElement)) 
                        ? cvvElement.GetString() : null,
                    ExpirationMonth = (accountType == "Card" && root.TryGetProperty("ExpirationMonth", out var expMonthElement)) 
                        ? expMonthElement.GetString() : null,
                    ExpirationYear = (accountType == "Card" && root.TryGetProperty("ExpirationYear", out var expYearElement)) 
                        ? expYearElement.GetString() : null,
                    
                    // E-Wallet fields - only parse if AccountType is EWallet
                    BankName = (accountType == "EWallet" && root.TryGetProperty("BankName", out var bankNameElement)) 
                        ? bankNameElement.GetString() : null,
                    AccountNumber = (accountType == "EWallet" && root.TryGetProperty("AccountNumber", out var accountNumberElement))
                        ? accountNumberElement.GetString() : null,
                    
                    IsDefault = root.TryGetProperty("IsDefault", out var isDefaultElement) && 
                            (isDefaultElement.ValueKind == JsonValueKind.True || 
                                (isDefaultElement.ValueKind == JsonValueKind.String && isDefaultElement.GetString() == "true")),
                    
                    // Address fields - optional for e-wallets, required for cards
                    Region = root.TryGetProperty("Region", out var regionElement) ? regionElement.GetString() : null,
                    Province = root.TryGetProperty("Province", out var provinceElement) ? provinceElement.GetString() : null,
                    City = root.TryGetProperty("City", out var cityElement) ? cityElement.GetString() : null,
                    Barangay = root.TryGetProperty("Barangay", out var barangayElement) ? barangayElement.GetString() : null,
                    StreetName = root.TryGetProperty("StreetName", out var streetElement) ? streetElement.GetString() : null,
                    Building = root.TryGetProperty("Building", out var buildingElement) ? buildingElement.GetString() : null,
                    HouseNo = root.TryGetProperty("HouseNo", out var houseElement) ? houseElement.GetString() : null
                };

                // Parse PostalCode separately (nullable int)
                if (root.TryGetProperty("PostalCode", out var postalElement) && postalElement.ValueKind != JsonValueKind.Null)
                {
                    if (postalElement.ValueKind == JsonValueKind.Number)
                    {
                        model.PostalCode = postalElement.GetInt32();
                    }
                    else
                    {
                        var postalStr = postalElement.GetString();
                        if (!string.IsNullOrEmpty(postalStr) && int.TryParse(postalStr, out int postal))
                        {
                            model.PostalCode = postal;
                        }
                    }
                }

                // Validate required fields based on account type
                if (accountType == "Card")
                {
                    if (string.IsNullOrEmpty(model.CardNumber))
                        return Json(new { success = false, message = "Card number is required" });
                    if (string.IsNullOrEmpty(model.ExpirationMonth))
                        return Json(new { success = false, message = "Expiration month is required" });
                    if (string.IsNullOrEmpty(model.ExpirationYear))
                        return Json(new { success = false, message = "Expiration year is required" });
                    if (string.IsNullOrEmpty(model.CvvCode))
                        return Json(new { success = false, message = "CVV is required" });
                }
                else if (accountType == "EWallet")
                {
                    if (string.IsNullOrEmpty(model.BankName))
                        return Json(new { success = false, message = "Provider is required" });
                    if (string.IsNullOrEmpty(model.AccountNumber))
                        return Json(new { success = false, message = "Account number is required" });
                }
                else
                {
                    return Json(new { success = false, message = "Invalid account type" });
                }

                // Account name is always required
                if (string.IsNullOrEmpty(model.AccountName))
                    return Json(new { success = false, message = "Account holder name is required" });

                // Check if this is an update or insert
                bool isUpdate = false;
                
                if (payoutAccountId.HasValue && payoutAccountId.Value > 0)
                {
                    // Verify that this record actually exists and belongs to the user
                    var existing = await _db.PayoutAccounts
                        .FromSqlRaw("SELECT * FROM Payout_Accounts WHERE Payout_Account_Id = @p0 AND User_Id = @p1", 
                            payoutAccountId.Value, userId.Value)
                        .FirstOrDefaultAsync();
                    
                    isUpdate = existing != null;
                }

                // Prepare parameters with null handling
                var parameters = new List<SqlParameter>
                {
                    new SqlParameter("@UserId", userId.Value),
                    new SqlParameter("@AccountType", accountType),
                    new SqlParameter("@AccountName", model.AccountName ?? ""),
                    new SqlParameter("@AccountNumber", accountType == "EWallet" ? (model.AccountNumber ?? "") : ""),
                    new SqlParameter("@CardNumber", accountType == "Card" ? (model.CardNumber ?? "") : ""),
                    new SqlParameter("@CvvCode", accountType == "Card" ? (model.CvvCode ?? "") : ""),
                    new SqlParameter("@ExpirationMonth", accountType == "Card" ? (model.ExpirationMonth ?? "") : ""),
                    new SqlParameter("@ExpirationYear", accountType == "Card" ? (model.ExpirationYear ?? "") : ""),
                    new SqlParameter("@BankName", accountType == "EWallet" ? (model.BankName ?? "") : ""),
                    new SqlParameter("@IsDefault", model.IsDefault),
                    new SqlParameter("@PostalCode", model.PostalCode.HasValue ? (object)model.PostalCode.Value : DBNull.Value),
                    new SqlParameter("@Region", model.Region ?? ""),
                    new SqlParameter("@Province", model.Province ?? ""),
                    new SqlParameter("@City", model.City ?? ""),
                    new SqlParameter("@Barangay", model.Barangay ?? ""),
                    new SqlParameter("@StreetName", model.StreetName ?? ""),
                    new SqlParameter("@Building", string.IsNullOrEmpty(model.Building) ? DBNull.Value : (object)model.Building),
                    new SqlParameter("@HouseNo", model.HouseNo ?? "")
                };

                if (isUpdate)
                {
                    // Update existing
                    var updateParams = new List<SqlParameter>
                    {
                        new SqlParameter("@PayoutAccountId", payoutAccountId!.Value)
                    };
                    updateParams.AddRange(parameters);
                    
                    await _db.Database.ExecuteSqlRawAsync(
                        "EXEC dbo.sp_PaymentMethods_Update @PayoutAccountId, @UserId, @AccountType, @AccountName, @AccountNumber, @CardNumber, @CvvCode, @ExpirationMonth, @ExpirationYear, @BankName, @IsDefault, @PostalCode, @Region, @Province, @City, @Barangay, @StreetName, @Building, @HouseNo",
                        updateParams.ToArray());

                    return Json(new { success = true, message = "Payment method updated successfully!" });
                }
                else
                {
                    // Insert new
                    await _db.Database.ExecuteSqlRawAsync(
                        "EXEC dbo.sp_PaymentMethods_Insert @UserId, @AccountType, @AccountName, @AccountNumber, @CardNumber, @CvvCode, @ExpirationMonth, @ExpirationYear, @BankName, @IsDefault, @PostalCode, @Region, @Province, @City, @Barangay, @StreetName, @Building, @HouseNo",
                        parameters.ToArray());

                    return Json(new { success = true, message = "Payment method added successfully!" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SavePaymentMethod: {ex}");
                return Json(new { success = false, message = ex.Message });
            }
        }  
                
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePaymentMethod(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "Not logged in" });
            }

            try
            {
                if (id <= 0)
                {
                    return Json(new { success = false, message = "Invalid payment method ID" });
                }

                var parameters = new[]
                {
                    new SqlParameter("@PayoutAccountId", id),
                    new SqlParameter("@UserId", userId.Value)
                };

                await _db.Database.ExecuteSqlRawAsync(
                    "EXEC dbo.sp_PaymentMethods_Delete @PayoutAccountId, @UserId",
                    parameters);

                return Json(new { success = true, message = "Payment method deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DebugPaymentMethod()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "Not logged in" });
            }

            try
            {
                // Read the raw request body
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();
                
                Console.WriteLine("RAW REQUEST BODY: " + body);
                
                // Try to parse it
                try {
                    var data = JsonSerializer.Deserialize<PaymentMethodViewModel>(body);
                    return Json(new { 
                        success = true, 
                        message = "Data received", 
                        rawData = body,
                        parsedData = data 
                    });
                }
                catch (Exception ex) {
                    return Json(new { 
                        success = false, 
                        message = "Failed to parse JSON", 
                        error = ex.Message,
                        rawData = body 
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetDefaultPaymentMethod(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "Not logged in" });
            }

            try
            {
                if (id <= 0)
                {
                    return Json(new { success = false, message = "Invalid payment method ID" });
                }

                var parameters = new[]
                {
                    new SqlParameter("@PayoutAccountId", id),
                    new SqlParameter("@UserId", userId.Value)
                };

                await _db.Database.ExecuteSqlRawAsync(
                    "EXEC dbo.sp_PaymentMethods_SetDefault @PayoutAccountId, @UserId",
                    parameters);

                return Json(new { success = true, message = "Default payment method updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // =========================
        // LOGIN GET
        // =========================
        [HttpGet]
        public IActionResult Login()
        {
            return View("~/Views/AccountProfile/Login.cshtml");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("~/Views/AccountProfile/Login.cshtml", model);
            }

            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == model.Email && u.IsActive);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Invalid email or password.");
                    return View("~/Views/AccountProfile/Login.cshtml", model);
                }

                if (string.IsNullOrEmpty(user.PasswordHash) || !user.PasswordHash.Contains(":"))
                {
                    ModelState.AddModelError(string.Empty, "Account configuration error. Please contact support.");
                    return View("~/Views/AccountProfile/Login.cshtml", model);
                }

                var parts = user.PasswordHash.Split(':');
                var storedHash = parts[0];
                var storedSalt = parts[1];
                byte[] saltBytes = Convert.FromBase64String(storedSalt);

                string enteredHash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                    password: model.Password,
                    salt: saltBytes,
                    prf: KeyDerivationPrf.HMACSHA256,
                    iterationCount: 10000,
                    numBytesRequested: 256 / 8));

                if (enteredHash != storedHash)
                {
                    ModelState.AddModelError(string.Empty, "Invalid email or password.");
                    return View("~/Views/AccountProfile/Login.cshtml", model);
                }

                HttpContext.Session.SetInt32("UserId", user.UserId);
                HttpContext.Session.SetString("UserEmail", user.Email);

                return RedirectToAction("ProfileView", "AccountProfile");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("~/Views/AccountProfile/Login.cshtml", model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUsername([FromBody] UsernameRequest request)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
                return Json(new { success = false, message = "User not logged in" });

            if (string.IsNullOrWhiteSpace(request.Username))
                return Json(new { success = false, message = "Username cannot be empty" });

            try
            {
                // Execute stored procedure to update username
                var parameters = new[]
                {
                    new SqlParameter("@UserId", userId.Value),
                    new SqlParameter("@Username", request.Username.Trim())
                };

                await _db.Database.ExecuteSqlRawAsync("EXEC dbo.sp_Profile_UpdateUsername @UserId, @Username", parameters);

                return Json(new { success = true, message = "Username updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class UsernameRequest
        {
            public string Username { get; set; } = "";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateField([FromBody] UpdateFieldViewModel model)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "Not logged in" });
                }

                string spName = GetStoredProcedureName(model.Field.ToLower());
                
                var parameters = new[]
                {
                    new SqlParameter("@UserId", userId.Value),
                    new SqlParameter("@Value", model.Value ?? "")
                };

                await _db.Database.ExecuteSqlRawAsync($"EXEC dbo.{spName} @UserId, @Value", parameters);

                // Get updated display value
                var displayValue = await GetDisplayValueFromDb(userId.Value, model.Field.ToLower());

                return Json(new 
                { 
                    success = true, 
                    message = $"{model.Field} updated successfully",
                    displayValue = displayValue
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateFullName([FromBody] UpdateNameViewModel model)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "Not logged in" });
                }

                // Execute stored procedure to update full name
                var parameters = new[]
                {
                    new SqlParameter("@UserId", userId.Value),
                    new SqlParameter("@FirstName", model.FirstName ?? ""),
                    new SqlParameter("@MiddleName", model.MiddleName ?? ""),
                    new SqlParameter("@LastName", model.LastName ?? "")
                };

                await _db.Database.ExecuteSqlRawAsync(
                    "EXEC dbo.sp_Profile_UpdateFullName @UserId, @FirstName, @MiddleName, @LastName", 
                    parameters);

                // Get updated full name
                var displayValue = await GetDisplayValueFromDb(userId.Value, "fullname");

                return Json(new 
                { 
                    success = true, 
                    message = "Name updated successfully",
                    displayValue = displayValue
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePassword([FromBody] UpdatePasswordViewModel model)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return Unauthorized(new { success = false, message = "Not logged in" });
                }

                var user = await _db.Users.FindAsync(userId.Value);
                if (user == null)
                {
                    return NotFound(new { success = false, message = "User not found" });
                }

                // Verify current password
                if (!VerifyPassword(model.CurrentPassword, user.PasswordHash))
                {
                    return BadRequest(new { success = false, message = "Current password is incorrect" });
                }

                // Hash new password
                string newPasswordHash = HashPassword(model.NewPassword);

                // Update password using stored procedure
                var parameters = new[]
                {
                    new SqlParameter("@UserId", userId.Value),
                    new SqlParameter("@PasswordHash", newPasswordHash)
                };

                await _db.Database.ExecuteSqlRawAsync("EXEC dbo.sp_Profile_UpdatePassword @UserId, @PasswordHash", parameters);

                return Json(new { success = true, message = "Password updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetProfileData()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return Unauthorized();
            }

            var user = await _db.Users
                .Include(u => u.Consumer)
                .FirstOrDefaultAsync(u => u.UserId == userId.Value);

            if (user == null)
            {
                return NotFound();
            }

            var data = new
            {
                firstName = user.Consumer?.FirstName ?? "",
                middleName = user.Consumer?.MiddleName ?? "",
                lastName = user.Consumer?.LastName ?? "",
                fullName = user.Consumer?.FullName ?? "Set now",
                username = user.Consumer?.Username ?? "Set now",
                address = user.Consumer?.Address ?? "Set now",
                phoneNumber = user.Consumer?.PhoneNumber ?? "Set now",
                email = user.Email ?? "Set now"
            };

            return Json(data);
        }

        // Helper methods
        private string GetStoredProcedureName(string field)
        {
            return field switch
            {
                "firstname" => "sp_Profile_UpdateFirstName",
                "middlename" => "sp_Profile_UpdateMiddleName",
                "lastname" => "sp_Profile_UpdateLastName",
                "username" => "sp_Profile_UpdateUsername",
                "address" => "sp_Profile_UpdateAddress",
                "phone" or "phonenumber" => "sp_Profile_UpdatePhoneNumber",
                "email" => "sp_Profile_UpdateEmail",
                _ => throw new ArgumentException($"Invalid field: {field}")
            };
        }

        private async Task<string> GetDisplayValueFromDb(int userId, string field)
        {
            var user = await _db.Users
                .Include(u => u.Consumer)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user?.Consumer == null)
                return "Set now";

            return field switch
            {
                "firstname" => user.Consumer.FirstName ?? "Set now",
                "middlename" => user.Consumer.MiddleName ?? "Set now",
                "lastname" => user.Consumer.LastName ?? "Set now",
                "fullname" => user.Consumer.FullName ?? "Set now",
                "username" => user.Consumer.Username ?? "Set now",
                "address" => user.Consumer.Address ?? "Set now",
                "phone" or "phonenumber" => user.Consumer.PhoneNumber ?? "Set now",
                "email" => user.Email ?? "Set now",
                _ => "Set now"
            };
        }

        private bool VerifyPassword(string password, string passwordHash)
        {
            if (string.IsNullOrEmpty(passwordHash) || !passwordHash.Contains(":"))
                return false;

            var parts = passwordHash.Split(':');
            var storedHash = parts[0];
            var storedSalt = parts[1];
            byte[] saltBytes = Convert.FromBase64String(storedSalt);

            string enteredHash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: saltBytes,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            return enteredHash == storedHash;
        }

        private string HashPassword(string password)
        {
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            string hash = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            return $"{hash}:{Convert.ToBase64String(salt)}";
        }
    }
}