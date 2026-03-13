using Microsoft.AspNetCore.Mvc;
using NextHorizon.Models;
using NextHorizon.Services;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace NextHorizon.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ISellerContextService _sellerContextService;
        private readonly ISellerPerformanceService _sellerPerformanceService;
        private readonly IConfiguration _configuration;

        public DashboardController(
            ISellerContextService sellerContextService,
            ISellerPerformanceService sellerPerformanceService,
            IConfiguration configuration)
        {
            _sellerContextService = sellerContextService;
            _sellerPerformanceService = sellerPerformanceService;
            _configuration = configuration;
        }

        private IActionResult? RedirectIfNotLoggedIn()
        {
            if (HttpContext.Session.GetString("SellerEmail") == null)
                return RedirectToAction("Login", "Account");
            return null;
        }

        private int? GetSellerIdFromSession()
        {
            // GetInt32 returns null if the key doesn't exist
            return HttpContext.Session.GetInt32("SellerId");
        }

        private string GetConnectionString()
        {
            return _configuration.GetConnectionString("DefaultConnection");
        }

        // ============== SELLER DASHBOARD ==============
        public async Task<IActionResult> SellerDashboard(CancellationToken cancellationToken)
        {
            var redirect = RedirectIfNotLoggedIn();
            if (redirect != null) return redirect;
            
            return View(await BuildSellerDashboardModelAsync(cancellationToken));
        }

        // ============== ORDER MANAGEMENT ==============
        public async Task<IActionResult> OrderManagement(CancellationToken cancellationToken)
        {
            var redirect = RedirectIfNotLoggedIn();
            if (redirect != null) return redirect;
            
            return View(await BuildSellerDashboardModelAsync(cancellationToken));
        }

        // ============== ORDER DETAILS ==============
        public async Task<IActionResult> OrderDetails(string id, CancellationToken cancellationToken)
        {
            var redirect = RedirectIfNotLoggedIn();
            if (redirect != null) return redirect;
            
            var dashboard = await BuildSellerDashboardModelAsync(cancellationToken);
            var order = dashboard.Orders.FirstOrDefault(o =>
                string.Equals(o.OrderId, id, StringComparison.OrdinalIgnoreCase));

            if (order is null)
            {
                return NotFound();
            }

            return View(BuildOrderDetailsModel(order, dashboard.SellerName));
        }

        private static OrderDetailsViewModel BuildOrderDetailsModel(Order order, string sellerName)
        {
            var shippingFee = order.Courier.Contains("LBC", StringComparison.OrdinalIgnoreCase) ? 120.00m : 85.00m;
            var discount = string.Equals(order.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase) ? 50.00m : 0.00m;
            var subtotal = order.TotalAmount;
            var unitPrice = order.Quantity > 0 ? decimal.Round(order.TotalAmount / order.Quantity, 2) : order.TotalAmount;

            return new OrderDetailsViewModel
            {
                SellerName = sellerName,
                OrderId = order.OrderId,
                BuyerName = order.Customer,
                OrderDateTime = order.DateTime,
                PaymentStatus = order.PaymentStatus,
                FulfillmentStatus = order.FulfillmentStatus,
                Courier = order.Courier,
                TrackingNumber = $"TRK-{order.OrderId.Replace("ORD-", string.Empty)}-{order.DateTime:MMdd}",
                ShippingAddress = "1208 Horizon Heights, Bonifacio Global City, Taguig City",
                ContactNumber = "+63 917 555 0123",
                Notes = "Leave parcel at concierge if receiver is unavailable.",
                Subtotal = subtotal,
                ShippingFee = shippingFee,
                Discount = discount,
                Items = new List<OrderLineItemViewModel>
                {
                    new OrderLineItemViewModel
                    {
                        ProductName = order.ProductName,
                        ProductImage = order.ProductImage,
                        Quantity = order.Quantity,
                        UnitPrice = unitPrice
                    }
                }
            };
        }

        // ============== FINANCE DASHBOARD ==============
        public async Task<IActionResult> Finance()
        {
            var redirect = RedirectIfNotLoggedIn();
            if (redirect != null) return redirect;

            var sellerId = GetSellerIdFromSession();
            if (sellerId == null) return RedirectToAction("Login", "Account");

            var model = await GetFinanceDashboardData(sellerId.Value);
            return View(model);
        }


        private async Task<FinanceViewModel> GetFinanceDashboardData(int sellerId)
        {
            var model = new FinanceViewModel();

            using (var connection = new SqlConnection(GetConnectionString()))
            {
                using (var command = new SqlCommand("sp_GetSellerFinanceDashboard", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@SellerId", sellerId);

                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        // Read Seller Info
                        if (await reader.ReadAsync())
                        {
                            model.SellerName = reader["SellerName"]?.ToString() ?? "Seller";
                            model.CurrentDate = reader["CurrentDate"] != DBNull.Value 
                                ? Convert.ToDateTime(reader["CurrentDate"]) 
                                : DateTime.Now;
                        }

                        // Next result set - Wallet Info
                        if (await reader.NextResultAsync() && await reader.ReadAsync())
                        {
                            model.AvailableBalance = reader["Available_Balance"] != DBNull.Value 
                                ? Convert.ToDecimal(reader["Available_Balance"]) : 0;
                            model.PendingBalance = reader["Pending_Balance"] != DBNull.Value 
                                ? Convert.ToDecimal(reader["Pending_Balance"]) : 0;
                            model.TotalEarned = reader["Total_Earned"] != DBNull.Value 
                                ? Convert.ToDecimal(reader["Total_Earned"]) : 0;
                            model.TotalWithdrawn = reader["Total_Withdrawn"] != DBNull.Value 
                                ? Convert.ToDecimal(reader["Total_Withdrawn"]) : 0;
                        }

                        // Next result set - Today's Revenue
                        if (await reader.NextResultAsync() && await reader.ReadAsync())
                        {
                            model.TodayRevenue = reader["TodayRevenue"] != DBNull.Value 
                                ? Convert.ToDecimal(reader["TodayRevenue"]) : 0;
                        }

                        // Next result set - This Month's Revenue
                        if (await reader.NextResultAsync() && await reader.ReadAsync())
                        {
                            model.ThisMonthRevenue = reader["ThisMonthRevenue"] != DBNull.Value 
                                ? Convert.ToDecimal(reader["ThisMonthRevenue"]) : 0;
                        }

                        // Next result set - Recent Transactions
                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                model.Transactions.Add(new FinanceTransactionViewModel
                                {
                                    ReferenceId = reader["ReferenceId"]?.ToString() ?? "",
                                    TransactionDate = reader["TransactionDate"] != DBNull.Value 
                                        ? Convert.ToDateTime(reader["TransactionDate"]) : DateTime.Now,
                                    Type = reader["Type"]?.ToString() ?? "",
                                    Method = reader["Method"]?.ToString() ?? "",
                                    Amount = reader["Amount"] != DBNull.Value 
                                        ? Convert.ToDecimal(reader["Amount"]) : 0,
                                    Status = reader["Status"]?.ToString() ?? ""
                                });
                            }
                        }
                    }
                }
            }

            return model;
        }

        // ============== TRANSACTION HISTORY ==============
        public async Task<IActionResult> TransactionHistory(
            int page = 1, 
            string type = null, 
            string status = null, 
            DateTime? fromDate = null, 
            DateTime? toDate = null)
        {
            var redirect = RedirectIfNotLoggedIn();
            if (redirect != null) return redirect;

            var sellerId = GetSellerIdFromSession();
            if (sellerId == null) return RedirectToAction("Login", "Account");

            var model = await GetTransactionHistory(sellerId.Value, page, 10, type, status, fromDate, toDate);
            return View(model);
        }

        private async Task<TransactionHistoryViewModel> GetTransactionHistory(
            int sellerId, 
            int pageNumber, 
            int pageSize,
            string type = null,
            string status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var model = new TransactionHistoryViewModel
            {
                CurrentPage = pageNumber,
                PageSize = pageSize,
                FilterType = type,
                FilterStatus = status,
                FilterFromDate = fromDate,
                FilterToDate = toDate
            };

            using (var connection = new SqlConnection(GetConnectionString()))
            {
                using (var command = new SqlCommand("sp_GetSellerTransactionHistory", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@SellerId", sellerId);
                    command.Parameters.AddWithValue("@PageNumber", pageNumber);
                    command.Parameters.AddWithValue("@PageSize", pageSize);
                    
                    if (!string.IsNullOrEmpty(type))
                        command.Parameters.AddWithValue("@TransactionType", type);
                    if (!string.IsNullOrEmpty(status))
                        command.Parameters.AddWithValue("@Status", status);
                    if (fromDate.HasValue)
                        command.Parameters.AddWithValue("@FromDate", fromDate.Value);
                    if (toDate.HasValue)
                        command.Parameters.AddWithValue("@ToDate", toDate.Value.AddDays(1).AddSeconds(-1));

                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        // Read Total Count
                        if (await reader.ReadAsync())
                        {
                            model.TotalCount = reader["TotalCount"] != DBNull.Value 
                                ? Convert.ToInt32(reader["TotalCount"]) : 0;
                        }

                        // Read Transactions
                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                model.Transactions.Add(new FinanceTransactionViewModel
                                {
                                    ReferenceId = reader["ReferenceId"]?.ToString() ?? "",
                                    TransactionDate = reader["TransactionDate"] != DBNull.Value 
                                        ? Convert.ToDateTime(reader["TransactionDate"]) : DateTime.Now,
                                    Type = reader["Type"]?.ToString() ?? "",
                                    Method = reader["Method"]?.ToString() ?? "",
                                    Amount = reader["Amount"] != DBNull.Value 
                                        ? Convert.ToDecimal(reader["Amount"]) : 0,
                                    Status = reader["Status"]?.ToString() ?? ""
                                });
                            }
                        }
                    }
                }
            }

            // Get seller name separately
            model.SellerName = await GetSellerName(sellerId);
            model.CurrentDate = DateTime.Now;

            return model;
        }

        // ============== MY BALANCE ==============
        public async Task<IActionResult> MyBalance()
        {
            var redirect = RedirectIfNotLoggedIn();
            if (redirect != null) return redirect;

            var sellerId = GetSellerIdFromSession();
            if (sellerId == null) return RedirectToAction("Login", "Account");

            var model = await GetBalanceDetails(sellerId.Value);
            return View(model);
        }

        private async Task<BalanceDetailsViewModel> GetBalanceDetails(int sellerId)
        {
            var model = new BalanceDetailsViewModel();

            using (var connection = new SqlConnection(GetConnectionString()))
            {
                using (var command = new SqlCommand("sp_GetSellerBalanceDetails", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@SellerId", sellerId);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            model.SellerName = reader["SellerName"]?.ToString() ?? "Seller";
                            model.CurrentDate = reader["CurrentDate"] != DBNull.Value 
                                ? Convert.ToDateTime(reader["CurrentDate"]) : DateTime.Now;
                            model.AvailableBalance = reader["Available_Balance"] != DBNull.Value 
                                ? Convert.ToDecimal(reader["Available_Balance"]) : 0;
                            model.PendingBalance = reader["Pending_Balance"] != DBNull.Value 
                                ? Convert.ToDecimal(reader["Pending_Balance"]) : 0;
                            model.TotalEarned = reader["Total_Earned"] != DBNull.Value 
                                ? Convert.ToDecimal(reader["Total_Earned"]) : 0;
                            model.TotalWithdrawn = reader["Total_Withdrawn"] != DBNull.Value 
                                ? Convert.ToDecimal(reader["Total_Withdrawn"]) : 0;
                            model.PendingPayoutCount = reader["PendingPayoutCount"] != DBNull.Value 
                                ? Convert.ToInt32(reader["PendingPayoutCount"]) : 0;
                            model.TotalPendingWithdrawal = reader["TotalPendingWithdrawal"] != DBNull.Value 
                                ? Convert.ToDecimal(reader["TotalPendingWithdrawal"]) : 0;
                        }

                        // Read recent transactions if available
                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                model.RecentTransactions.Add(new FinanceTransactionViewModel
                                {
                                    ReferenceId = reader["ReferenceId"]?.ToString() ?? "",
                                    TransactionDate = reader["TransactionDate"] != DBNull.Value 
                                        ? Convert.ToDateTime(reader["TransactionDate"]) : DateTime.Now,
                                    Type = reader["Type"]?.ToString() ?? "",
                                    Method = reader["Method"]?.ToString() ?? "",
                                    Amount = reader["Amount"] != DBNull.Value 
                                        ? Convert.ToDecimal(reader["Amount"]) : 0,
                                    Status = reader["Status"]?.ToString() ?? ""
                                });
                            }
                        }
                    }
                }
            }

            return model;
        }

        // ============== PAYOUT ACCOUNTS ==============
        public async Task<IActionResult> PayoutAccounts()
        {
            var redirect = RedirectIfNotLoggedIn();
            if (redirect != null) return redirect;

            var sellerId = GetSellerIdFromSession();
            if (sellerId == null) return RedirectToAction("Login", "Account");

            var accounts = await GetPayoutAccounts(sellerId.Value);
            ViewBag.SellerName = await GetSellerName(sellerId.Value);
            
            // Pass the model to the view
            return View(accounts);
        }

        private async Task<List<PayoutAccountViewModel>> GetPayoutAccounts(int sellerId)
        {
            var accounts = new List<PayoutAccountViewModel>();

            using (var connection = new SqlConnection(GetConnectionString()))
            {
                using (var command = new SqlCommand("sp_GetSellerPayoutAccounts", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@SellerId", sellerId);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            accounts.Add(new PayoutAccountViewModel
                            {
                                AccountId = reader["AccountId"] != DBNull.Value ? Convert.ToInt32(reader["AccountId"]) : 0,
                                AccountType = reader["AccountType"]?.ToString() ?? "",
                                AccountName = reader["AccountName"]?.ToString() ?? "",
                                AccountNumber = reader["AccountNumber"]?.ToString() ?? "",
                                BankName = reader["BankName"]?.ToString(),
                                CardNumber = reader["CardNumber"]?.ToString(),
                                LastUsed = reader["LastUsed"] != DBNull.Value ? Convert.ToDateTime(reader["LastUsed"]) : (DateTime?)null,
                                IsDefault = reader["IsDefault"] != DBNull.Value && Convert.ToBoolean(reader["IsDefault"])
                            });
                        }
                    }
                }
            }

            return accounts;
        }

        // Add these methods to your DashboardController.cs

// ============== SET DEFAULT PAYOUT ACCOUNT ==============
[HttpPost]
[HttpPost]
public async Task<IActionResult> SetDefaultPayoutAccount([FromBody] SetDefaultAccountRequest request)
{
    var redirect = RedirectIfNotLoggedIn();
    if (redirect != null) return Unauthorized();

    var sellerId = GetSellerIdFromSession();
    if (sellerId == null) return Unauthorized();

    try
    {
        using (var connection = new SqlConnection(GetConnectionString()))
        {
            using (var command = new SqlCommand("sp_SetDefaultPayoutAccount", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@AccountId", request.AccountId);
                command.Parameters.AddWithValue("@SellerId", sellerId.Value);

                await connection.OpenAsync();
                await command.ExecuteNonQueryAsync();
            }
        }

        return Ok();
    }
    catch (Exception ex)
    {
        return StatusCode(500, ex.Message);
    }
}


// ============== REMOVE PAYOUT ACCOUNT ==============

[HttpPost]
public async Task<IActionResult> RemovePayoutAccount([FromBody] RemoveAccountRequest request)
{
    var redirect = RedirectIfNotLoggedIn();
    if (redirect != null) return Unauthorized();

    var sellerId = GetSellerIdFromSession();
    if (sellerId == null) return Unauthorized();

    try
    {
        using (var connection = new SqlConnection(GetConnectionString()))
        {
            using (var command = new SqlCommand("sp_RemovePayoutAccount", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@AccountId", request.AccountId);
                command.Parameters.AddWithValue("@SellerId", sellerId.Value);

                await connection.OpenAsync();
                
                // Execute and read the returned value
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        int rowsAffected = reader.GetInt32(0);
                        
                        // Always return 200 OK with success flag
                        return Ok(new { 
                            success = rowsAffected > 0, 
                            message = rowsAffected > 0 ? "Account removed successfully" : "Account not found or already removed" 
                        });
                    }
                }
                
                // If no result returned
                return Ok(new { success = false, message = "No response from database" });
            }
        }
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { success = false, message = ex.Message });
    }
}


// Update the POST AddPayoutAccount method
// ============== ADD PAYOUT ACCOUNT (GET) ==============
[HttpGet]
public IActionResult AddPayoutAccount()
{
    var redirect = RedirectIfNotLoggedIn();
    if (redirect != null) return redirect;

    return View();
}

// ============== ADD PAYOUT ACCOUNT (POST) ==============
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> AddPayoutAccount(AddPayoutAccountViewModel model)
{
    var redirect = RedirectIfNotLoggedIn();
    if (redirect != null) return redirect;

    var sellerId = GetSellerIdFromSession();
    if (sellerId == null) return RedirectToAction("Login", "Account");

    try
    {
        using (var connection = new SqlConnection(GetConnectionString()))
        {
            using (var command = new SqlCommand("sp_AddPayoutAccount", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                
                command.Parameters.AddWithValue("@SellerId", sellerId.Value);
                command.Parameters.AddWithValue("@AccountType", model.AccountType ?? "");
                
                // Set AccountName and AccountNumber based on account type
                if (model.AccountType == "ewallet")
                {
                    command.Parameters.AddWithValue("@AccountName", model.EwalletAccountName ?? "");
                    command.Parameters.AddWithValue("@AccountNumber", model.EwalletAccountNumber ?? "");
                    command.Parameters.AddWithValue("@BankName", model.EWalletType ?? "E-Wallet");
                }
                else if (model.AccountType == "bank")
                {
                    command.Parameters.AddWithValue("@AccountName", model.BankAccountName ?? "");
                    command.Parameters.AddWithValue("@AccountNumber", model.BankAccountNumber ?? "");
                    command.Parameters.AddWithValue("@BankName", model.BankName ?? "");
                }
                else if (model.AccountType == "card")
                {
                    command.Parameters.AddWithValue("@AccountName", "Card Holder");
                    command.Parameters.AddWithValue("@AccountNumber", model.CardNumber ?? "");
                    command.Parameters.AddWithValue("@BankName", "Credit Card");
                }
                else
                {
                    command.Parameters.AddWithValue("@AccountName", model.AccountName ?? "");
                    command.Parameters.AddWithValue("@AccountNumber", model.AccountNumber ?? "");
                    command.Parameters.AddWithValue("@BankName", DBNull.Value);
                }
                
                // Handle optional fields
                command.Parameters.AddWithValue("@CardNumber", string.IsNullOrEmpty(model.CardNumber) ? DBNull.Value : (object)model.CardNumber);
                command.Parameters.AddWithValue("@CvvCode", string.IsNullOrEmpty(model.CVV) ? DBNull.Value : (object)model.CVV);
                
                // Handle expiry date
                if (!string.IsNullOrEmpty(model.ExpiryDate) && model.ExpiryDate.Contains('/'))
                {
                    var expiryParts = model.ExpiryDate.Split('/');
                    command.Parameters.AddWithValue("@ExpirationMonth", expiryParts[0]);
                    command.Parameters.AddWithValue("@ExpirationYear", expiryParts.Length > 1 ? expiryParts[1] : DBNull.Value);
                }
                else
                {
                    command.Parameters.AddWithValue("@ExpirationMonth", DBNull.Value);
                    command.Parameters.AddWithValue("@ExpirationYear", DBNull.Value);
                }
                
                // Address fields
                command.Parameters.AddWithValue("@PostalCode", string.IsNullOrEmpty(model.PostalCode) ? DBNull.Value : (object)Convert.ToInt32(model.PostalCode));
                command.Parameters.AddWithValue("@Region", string.IsNullOrEmpty(model.Region) ? DBNull.Value : (object)model.Region);
                command.Parameters.AddWithValue("@Province", string.IsNullOrEmpty(model.Province) ? DBNull.Value : (object)model.Province);
                command.Parameters.AddWithValue("@City", string.IsNullOrEmpty(model.City) ? DBNull.Value : (object)model.City);
                command.Parameters.AddWithValue("@Barangay", string.IsNullOrEmpty(model.Barangay) ? DBNull.Value : (object)model.Barangay);
                command.Parameters.AddWithValue("@StreetName", string.IsNullOrEmpty(model.StreetName) ? DBNull.Value : (object)model.StreetName);
                command.Parameters.AddWithValue("@Building", string.IsNullOrEmpty(model.Building) ? DBNull.Value : (object)model.Building);
                command.Parameters.AddWithValue("@HouseNo", string.IsNullOrEmpty(model.HouseNo) ? DBNull.Value : (object)model.HouseNo);
                command.Parameters.AddWithValue("@IsDefault", model.IsDefault);

                await connection.OpenAsync();
                var newAccountId = await command.ExecuteScalarAsync();
            }
        }

        TempData["SuccessMessage"] = "Payout account added successfully";
        return RedirectToAction("PayoutAccounts");
    }
    catch (Exception ex)
    {
        ViewBag.ErrorMessage = $"Error adding account: {ex.Message}";
        return View(model);
    }
}
    // ============== WITHDRAW (GET) ==============
    public async Task<IActionResult> Withdraw()
    {
        var redirect = RedirectIfNotLoggedIn();
        if (redirect != null) return redirect;

        var sellerId = GetSellerIdFromSession();
        if (sellerId == null) return RedirectToAction("Login", "Account");

        var model = await GetWithdrawalDetails(sellerId.Value);
        return View(model);
    }

    private async Task<WithdrawalDetailsViewModel> GetWithdrawalDetails(int sellerId)
    {
        var model = new WithdrawalDetailsViewModel();

        using (var connection = new SqlConnection(GetConnectionString()))
        {
            using (var command = new SqlCommand("sp_GetWithdrawalDetails", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@SellerId", sellerId);

                await connection.OpenAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    // Read seller info and wallet balance
                    if (await reader.ReadAsync())
                    {
                        model.SellerName = reader["SellerName"]?.ToString() ?? "Seller";
                        model.AvailableBalance = reader["AvailableBalance"] != DBNull.Value 
                            ? Convert.ToDecimal(reader["AvailableBalance"]) : 0;
                        model.PendingBalance = reader["PendingBalance"] != DBNull.Value 
                            ? Convert.ToDecimal(reader["PendingBalance"]) : 0;
                        model.TotalEarned = reader["TotalEarned"] != DBNull.Value 
                            ? Convert.ToDecimal(reader["TotalEarned"]) : 0;
                        model.TotalWithdrawn = reader["TotalWithdrawn"] != DBNull.Value 
                            ? Convert.ToDecimal(reader["TotalWithdrawn"]) : 0;
                    }

                    // Read payout accounts
                    if (await reader.NextResultAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            model.PayoutAccounts.Add(new PayoutAccountViewModel
                            {
                                AccountId = reader["AccountId"] != DBNull.Value ? Convert.ToInt32(reader["AccountId"]) : 0,
                                AccountType = reader["AccountType"]?.ToString() ?? "",
                                AccountName = reader["AccountName"]?.ToString() ?? "",
                                AccountNumber = reader["AccountNumber"]?.ToString() ?? "",
                                BankName = reader["BankName"]?.ToString(),
                                IsDefault = reader["IsDefault"] != DBNull.Value && Convert.ToBoolean(reader["IsDefault"])
                            });
                        }
                    }

                    // Read recent withdrawals
                    if (await reader.NextResultAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            model.RecentWithdrawals.Add(new RecentWithdrawalViewModel
                            {
                                WithdrawalId = reader["WithdrawalId"] != DBNull.Value ? Convert.ToInt64(reader["WithdrawalId"]) : 0,
                                Amount = reader["Amount"] != DBNull.Value ? Convert.ToDecimal(reader["Amount"]) : 0,
                                Status = reader["Status"]?.ToString() ?? "",
                                RequestedAt = reader["RequestedAt"] != DBNull.Value ? Convert.ToDateTime(reader["RequestedAt"]) : DateTime.Now
                            });
                        }
                    }
                }
            }
        }

        return model;
    }

    // ============== PROCESS WITHDRAWAL (POST) ==============
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessWithdrawal(WithdrawalRequestModel model)
    {
        var redirect = RedirectIfNotLoggedIn();
        if (redirect != null) return redirect;

        var sellerId = GetSellerIdFromSession();
        if (sellerId == null) return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid)
        {
            var details = await GetWithdrawalDetails(sellerId.Value);
            return View("Withdraw", details);
        }

        try
        {
            using (var connection = new SqlConnection(GetConnectionString()))
            {
                using (var command = new SqlCommand("sp_ProcessWithdrawal", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@SellerId", sellerId.Value);
                    command.Parameters.AddWithValue("@Amount", model.Amount);
                    command.Parameters.AddWithValue("@PayoutAccountId", model.PayoutAccountId);

                    var refNoParam = new SqlParameter("@ReferenceNo", SqlDbType.NVarChar, 50)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(refNoParam);

                    await connection.OpenAsync();
                    await command.ExecuteNonQueryAsync();

                    string referenceNo = refNoParam.Value?.ToString() ?? "";
                    TempData["SuccessMessage"] = $"Withdrawal request submitted successfully. Reference: {referenceNo}";
                    
                    return RedirectToAction("Finance");
                }
            }
        }
        catch (SqlException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error processing withdrawal: {ex.Message}";
        }

        var withdrawalDetails = await GetWithdrawalDetails(sellerId.Value);
        return View("Withdraw", withdrawalDetails);
    }

    // ============== WITHDRAWAL HISTORY ==============
    public async Task<IActionResult> WithdrawalHistory(int page = 1, string status = null)
    {
        var redirect = RedirectIfNotLoggedIn();
        if (redirect != null) return redirect;

        var sellerId = GetSellerIdFromSession();
        if (sellerId == null) return RedirectToAction("Login", "Account");

        var model = await GetWithdrawalHistory(sellerId.Value, page, 10, status);
        return View(model);
    }

    private async Task<WithdrawalHistoryViewModel> GetWithdrawalHistory(int sellerId, int pageNumber, int pageSize, string status = null)
    {
        var model = new WithdrawalHistoryViewModel
        {
            CurrentPage = pageNumber,
            PageSize = pageSize,
            FilterStatus = status
        };

        using (var connection = new SqlConnection(GetConnectionString()))
        {
            using (var command = new SqlCommand("sp_GetWithdrawalHistory", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@SellerId", sellerId);
                command.Parameters.AddWithValue("@PageNumber", pageNumber);
                command.Parameters.AddWithValue("@PageSize", pageSize);
                
                if (!string.IsNullOrEmpty(status))
                    command.Parameters.AddWithValue("@Status", status);

                await connection.OpenAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    // Read total count
                    if (await reader.ReadAsync())
                    {
                        model.TotalCount = reader["TotalCount"] != DBNull.Value 
                            ? Convert.ToInt32(reader["TotalCount"]) : 0;
                    }

                    // Read withdrawals
                    if (await reader.NextResultAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            model.Withdrawals.Add(new WithdrawalHistoryItem
                            {
                                WithdrawalId = reader["WithdrawalId"] != DBNull.Value ? Convert.ToInt64(reader["WithdrawalId"]) : 0,
                                Amount = reader["Amount"] != DBNull.Value ? Convert.ToDecimal(reader["Amount"]) : 0,
                                Status = reader["Status"]?.ToString() ?? "",
                                RequestedAt = reader["RequestedAt"] != DBNull.Value ? Convert.ToDateTime(reader["RequestedAt"]) : DateTime.Now,
                                ProcessedAt = reader["ProcessedAt"] != DBNull.Value ? Convert.ToDateTime(reader["ProcessedAt"]) : (DateTime?)null
                            });
                        }
                    }
                }
            }
        }

        return model;
    }
        // ============== ANALYTICS ==============
        public async Task<IActionResult> Analytics(CancellationToken cancellationToken)
        {
            var redirect = RedirectIfNotLoggedIn();
            if (redirect != null) return redirect;
            
            return View(await BuildSellerDashboardModelAsync(cancellationToken));
        }

        // ============== ACCOUNT SETTINGS ==============
        public IActionResult AccountSettings()
        {
            var redirect = RedirectIfNotLoggedIn();
            if (redirect != null) return redirect;
            
            return View();
        }

        // ============== HELPER METHODS ==============
        private async Task<decimal> GetAvailableBalance(int sellerId)
        {
            using (var connection = new SqlConnection(GetConnectionString()))
            {
                var query = "SELECT Available_Balance FROM seller_wallet WHERE Seller_Id = @SellerId";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@SellerId", sellerId);
                    await connection.OpenAsync();
                    var result = await command.ExecuteScalarAsync();
                    return result != DBNull.Value ? Convert.ToDecimal(result) : 0;
                }
            }
        }

        private async Task<string> GetSellerName(int sellerId)
        {
            using (var connection = new SqlConnection(GetConnectionString()))
            {
                var query = "SELECT business_name FROM sellers WHERE seller_id = @SellerId";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@SellerId", sellerId);
                    await connection.OpenAsync();
                    return (await command.ExecuteScalarAsync())?.ToString() ?? "Seller";
                }
            }
        }

        private async Task<int?> GetSellerIdByEmail(string email)
        {
            using (var connection = new SqlConnection(GetConnectionString()))
            {
                // Use 'sellers' table (plural)
                var query = "SELECT seller_id FROM sellers WHERE business_email = @Email";
                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Email", email);
                    await connection.OpenAsync();
                    var result = await command.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value)
                    {
                        return Convert.ToInt32(result);
                    }
                }
            }
            return null;
        }

        private async Task<SellerDashboardViewModel> BuildSellerDashboardModelAsync(CancellationToken cancellationToken = default)
        {
            var sellerEmail = HttpContext.Session.GetString("SellerEmail");
            var sellerContext = await _sellerContextService.ResolveSellerAsync(sellerEmail, cancellationToken);
            var performance = await _sellerPerformanceService.GetSellerPerformanceAsync(
                sellerContext.SellerId,
                DateTime.UtcNow,
                cancellationToken);
            var monthlyRevenue = await _sellerPerformanceService.GetMonthlyRevenueByYearAsync(
                sellerContext.SellerId,
                new[] { 2024, 2025, 2026 },
                cancellationToken);

            return new SellerDashboardViewModel
            {
                SellerName = sellerContext.SellerName,
                CurrentDate = DateTime.Now,

                OrdersToShip = 14,
                PendingOrders = 5,
                LowStockAlerts = 8,
                WithdrawAmount = 15400.00m,
                WithdrawStatus = "Processing",

                TodaySales = performance.TodaySales,
                TodayUnitsSold = performance.TodayUnitsSold,
                SalesGrowth = performance.SalesGrowth,
                TotalRevenue = performance.TotalRevenue,
                TotalVisits = 423,
                MonthlyRevenueByYear = monthlyRevenue,

                RecentOrders = new List<Order>
                {
                    new Order { OrderId = "061231", Customer = "Kiboy", Status = "Paid", Amount = 650.00m },
                    new Order { OrderId = "061232", Customer = "Loyd", Status = "To Ship", Amount = 650.00m },
                    new Order { OrderId = "061233", Customer = "Sarah", Status = "Pending", Amount = 650.00m },
                    new Order { OrderId = "061234", Customer = "Discaya", Status = "To Ship", Amount = 650.00m },
                    new Order { OrderId = "061235", Customer = "Romualdez", Status = "Pending", Amount = 650.00m },
                    new Order { OrderId = "061236", Customer = "Vins", Status = "Paid", Amount = 650.00m },
                    new Order { OrderId = "061237", Customer = "Kenneth", Status = "To Ship", Amount = 650.00m }
                },

                Orders = new List<Order>
                {
                    new Order
                    {
                        OrderId = "ORD-1001",
                        Customer = "Kiboy",
                        DateTime = DateTime.Now.AddHours(-4),
                        ProductImage = "https://picsum.photos/seed/order-1/80/80",
                        ProductName = "Nike Shoes",
                        Quantity = 1,
                        TotalAmount = 2650.00m,
                        PaymentStatus = "Paid",
                        FulfillmentStatus = "Pending",
                        Courier = "J&T Express"
                    },
                    new Order
                    {
                        OrderId = "ORD-1002",
                        Customer = "Loyd",
                        DateTime = DateTime.Now.AddHours(-3),
                        ProductImage = "https://picsum.photos/seed/order-2/80/80",
                        ProductName = "Croptop",
                        Quantity = 2,
                        TotalAmount = 1300.00m,
                        PaymentStatus = "COD",
                        FulfillmentStatus = "To Ship",
                        Courier = "Ninja Van"
                    },
                    new Order
                    {
                        OrderId = "ORD-1003",
                        Customer = "Sarah",
                        DateTime = DateTime.Now.AddHours(-2),
                        ProductImage = "https://picsum.photos/seed/order-3/80/80",
                        ProductName = "Tumbler",
                        Quantity = 3,
                        TotalAmount = 1950.00m,
                        PaymentStatus = "COD",
                        FulfillmentStatus = "Ship",
                        Courier = "LBC"
                    },
                    new Order
                    {
                        OrderId = "ORD-1004",
                        Customer = "Discaya",
                        DateTime = DateTime.Now.AddHours(-6),
                        ProductImage = "https://picsum.photos/seed/order-4/80/80",
                        ProductName = "Running Shorts",
                        Quantity = 1,
                        TotalAmount = 850.00m,
                        PaymentStatus = "COD",
                        FulfillmentStatus = "Delivered",
                        Courier = "LBC"
                    },
                    new Order
                    {
                        OrderId = "ORD-1005",
                        Customer = "Romualdez",
                        DateTime = DateTime.Now.AddHours(-10),
                        ProductImage = "https://picsum.photos/seed/order-5/80/80",
                        ProductName = "Sports Bottle",
                        Quantity = 1,
                        TotalAmount = 450.00m,
                        PaymentStatus = "Paid",
                        FulfillmentStatus = "Cancelled",
                        Courier = "J&T Express"
                    },
                    new Order
                    {
                        OrderId = "ORD-1006",
                        Customer = "Kenneth",
                        DateTime = DateTime.Now.AddHours(-12),
                        ProductImage = "https://picsum.photos/seed/order-6/80/80",
                        ProductName = "Training Shirt",
                        Quantity = 2,
                        TotalAmount = 1200.00m,
                        PaymentStatus = "Paid",
                        FulfillmentStatus = "Return",
                        Courier = "Ninja Van"
                    }
                },

                TopProducts = new List<TopSellingProduct>
                {
                    new TopSellingProduct { ProductName = "Nike Shoes", ImageUrl = "https://picsum.photos/seed/nike-shoes/120/120", UnitsSold = 16, Rank = 1 },
                    new TopSellingProduct { ProductName = "Croptop", ImageUrl = "https://picsum.photos/seed/croptop/120/120", UnitsSold = 12, Rank = 2 },
                    new TopSellingProduct { ProductName = "Tumbler", ImageUrl = "https://picsum.photos/seed/tumbler/120/120", UnitsSold = 6, Rank = 3 },
                    new TopSellingProduct { ProductName = "3 Men's Sleeveless", ImageUrl = "https://picsum.photos/seed/sleeveless/120/120", UnitsSold = 4, Rank = 4 }
                }
            };
        }
    }
}