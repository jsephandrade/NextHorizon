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
using NextHorizon.Data;
using Microsoft.EntityFrameworkCore;
namespace NextHorizon.Controllers
{
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IOrderService _orderService;
        private readonly ISellerContextService _sellerContextService;
        private readonly ISellerPerformanceService _sellerPerformanceService;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;
        public DashboardController(
            ISellerContextService sellerContextService,
            ISellerPerformanceService sellerPerformanceService,
            IConfiguration configuration,
            IOrderService orderService,
            AppDbContext context,
            IWebHostEnvironment environment)
            
        {
            _sellerContextService = sellerContextService;
            _sellerPerformanceService = sellerPerformanceService;
            _configuration = configuration;
            _orderService = orderService;
            _context = context;
            _environment = environment;
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
    int? currentSellerId = HttpContext.Session.GetInt32("SellerId");
    if (currentSellerId == null)
    {
        return RedirectToAction("Login", "Account");
    }

    var realOrders = await _orderService.GetOrdersBySellerAsync(currentSellerId.Value);
    var couriers = await _orderService.GetCouriersAsync();
    ViewBag.Couriers = couriers;
    return View(realOrders);
    }
    [HttpGet]
public async Task<IActionResult> GetOrderDetails(int orderId)
{
    // Security Check
    int? currentSellerId = HttpContext.Session.GetInt32("SellerId");
    if (currentSellerId == null) return Unauthorized();

var order = await _orderService.GetOrderByIdAsync(orderId, currentSellerId.Value);
    if (order == null) return NotFound(new { success = false, message = "Order not found" });

        // Map to a clean object to avoid JSON Circular Reference Exceptions (HTTP 500)
        // and precisely match the JavaScript frontend's expected properties.
        var safeData = new
        {
            orderId = order.OrderID,
            orderDate = order.OrderDate,
            paymentMethod = order.PaymentMethod,
            fullName = order.FullName,
            streetAddress = order.StreetAddress,
            city = order.City,
            postalCode = order.PostalCode,
            phoneNumber = order.PhoneNumber,
            deliveryOption = order.Courier ?? "Standard",
            quantity = order.Quantity,
            subtotal = order.Subtotal,
            shippingFee = order.ShippingFee,
            totalAmount = order.Subtotal + order.ShippingFee,
            productName = order.ProductName
        };

        return Json(new { success = true, data = safeData });
}
public class OrderNoteRequest
{
    public int OrderId { get; set; }
    public string Note { get; set; } = string.Empty;
}

[HttpPost]
public async Task<IActionResult> SaveOrderNote([FromBody] OrderNoteRequest request)
{
    int? currentSellerId = HttpContext.Session.GetInt32("SellerId");
    if (!currentSellerId.HasValue) return Json(new { success = false, message = "Session expired." });

    var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderID == request.OrderId);
    if (order == null || order.seller_id != currentSellerId.Value)
    {
        return Json(new { success = false, message = "Order not found or unauthorized." });
    }
    order.SellerNote = request.Note;
    try
    {
        await _context.SaveChangesAsync();
        return Json(new { success = true, message = "Note saved successfully!" });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, message = "Database error occurred." });
    }
}
[HttpPost]
public async Task<IActionResult> AcceptOrder([FromBody] AcceptOrderRequest request)
{
    // 1. Get the dynamic Seller ID from the session (Security Check)
    int? currentSellerId = HttpContext.Session.GetInt32("SellerId");
    if (currentSellerId == null)
    {
        return Json(new { success = false, message = "Session expired. Please log in again." });
    }

    // 2. Call our shiny new service method
    bool isSuccess = await _orderService.AcceptOrderAsync(request.OrderId, currentSellerId.Value, request.Courier);

    // 3. Tell the frontend if it worked!
    if (isSuccess)
    {
        return Json(new { success = true, message = "Order successfully moved to To Ship!" });
    }
    else
    {
        return Json(new { success = false, message = "Failed to accept order. Order not found." });
    }
}
public class MarkShippedRequest
{
    public int OrderId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    // IFormFile is what C# uses to catch the uploaded image
    public IFormFile? ProofOfShipment { get; set; }
}
[HttpPost]
public async Task<IActionResult> MarkOrderShipped([FromForm] MarkShippedRequest request)
{
    int? currentSellerId = HttpContext.Session.GetInt32("SellerId");
    if (!currentSellerId.HasValue)
        return Json(new { success = false, message = "Session expired. Please log in again." });

    var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderID == request.OrderId);
    if (order == null || order.seller_id != currentSellerId.Value)
    {
        return Json(new { success = false, message = "Order not found or unauthorized." });
    }

    // --- FILE SAVING BLOCK ---
    if (request.ProofOfShipment != null && request.ProofOfShipment.Length > 0)
    {
        try 
        {
            string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "receipts");
            
            if (!Directory.Exists(uploadsFolder)) 
            {
                Directory.CreateDirectory(uploadsFolder);
            }
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetExtension(request.ProofOfShipment.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await request.ProofOfShipment.CopyToAsync(fileStream);
            }

            // INSERTED HERE: Update the order object with the new path
            order.ProofOfShipmentUrl = "/uploads/receipts/" + uniqueFileName;
        }
        catch (Exception ex)
        {
            Console.WriteLine("File upload failed: " + ex.Message);
            // Optionally: return Json(new { success = false, message = "File upload failed." });
        }
    }

    // Dual-Tracking Architecture
    order.Status = "Shipped";              
    order.FulfillmentStatus = "Shipped";   
    order.TrackingNumber = request.TrackingNumber; 
    order.DateShipped = DateTime.Now;      

    try
    {
        // This saves BOTH the status changes AND the ProofOfShipmentUrl
        await _context.SaveChangesAsync();
        return Json(new { success = true, message = "Order marked as shipped successfully!" });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, message = "Database error occurred." });
    }
}
public class ShipmentUpdateModel
{
    public int OrderId { get; set; }
    public string TrackingNumber { get; set; }
}
// ==========================================
// ORDER DETAILS
// ==========================================
[HttpGet]
public async Task<IActionResult> OrderDetails(string id, CancellationToken cancellationToken)
{
    var redirect = RedirectIfNotLoggedIn();
    if (redirect != null) return redirect;
    
    if (!int.TryParse(id.Replace("ORD-", ""), out int orderId))
    {
        return BadRequest("Invalid Order ID");
    }

    var order = await _context.Orders
        .Include(o => o.OrderItems)
        .ThenInclude(i => i.Product) 
        .FirstOrDefaultAsync(o => o.OrderID == orderId);

    if (order == null)
    {
        return NotFound();
    }

    // ==========================================
    // 3. THE COURIER FIX (AGGRESSIVE LOOKUP)
    // ==========================================
    if (order.logistics_id.HasValue && order.logistics_id.Value > 0)
    {
        var courierName = await _context.Logistics
            .Where(l => l.logistics_id == order.logistics_id.Value)
            .Select(l => l.courier_name)
            .FirstOrDefaultAsync();

        order.Courier = string.IsNullOrEmpty(courierName) ? "NextHorizon Partner" : courierName; 
    }
    else
    {
        order.Courier = "NextHorizon Partner";
    }

    return View(order);
}
    

        private static OrderDetailsViewModel BuildOrderDetailsModel(Order order, string sellerName)
{
    // Combine the new address columns we added to the database
    var fullAddress = $"{order.StreetAddress}, {order.City} {order.PostalCode}".Trim().Trim(',');
    if (string.IsNullOrWhiteSpace(fullAddress)) 
    {
        fullAddress = "No address provided";
    }

    // Calculate unit price safely
    var unitPrice = order.Quantity > 0 ? decimal.Round(order.Subtotal / order.Quantity, 2) : order.Subtotal;

    return new OrderDetailsViewModel
    {
        SellerName = sellerName,
        OrderId = order.OrderID.ToString(),                
        BuyerName = order.FullName,
        OrderDateTime = order.OrderDate,
        PaymentStatus = order.PaymentMethod,
        FulfillmentStatus = order.Status,
        
        
        Courier = order.Courier ?? "Not Selected",
        TrackingNumber = order.TrackingNumber ?? "Pending Tracking",
        ShippingAddress = fullAddress,
        ContactNumber = order.PhoneNumber ?? "No phone number",
        Notes = "No special instructions.", 
        
        Subtotal = order.Subtotal,
        ShippingFee = order.ShippingFee,
        Discount = 0.00m, 
        Items = new List<OrderLineItemViewModel>
        {
            new OrderLineItemViewModel
            {
                ProductName = order.ProductName,
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
                            
                            // Get pending payout count and amount from withdrawal_requests
                            model.PendingPayoutCount = reader["PendingPayoutCount"] != DBNull.Value 
                                ? Convert.ToInt32(reader["PendingPayoutCount"]) : 0;
                            model.TotalPendingWithdrawal = reader["TotalPendingWithdrawal"] != DBNull.Value 
                                ? Convert.ToDecimal(reader["TotalPendingWithdrawal"]) : 0;
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

                        // Next result set - Recent Transactions (Combined)
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

        [HttpGet]
        public async Task<IActionResult> GetTransactionDetailsSP(string referenceId)
        {
            try
            {
                Console.WriteLine($"GetTransactionDetailsSP called with referenceId: {referenceId}");
                
                var sellerId = GetSellerIdFromSession();
                if (sellerId == null)
                {
                    return Json(new { success = false, message = "Seller not authenticated" });
                }

                Console.WriteLine($"SellerId: {sellerId}");

                var transaction = await GetTransactionDetailsFromSP(sellerId.Value, referenceId);

                if (transaction == null)
                {
                    Console.WriteLine($"No transaction found for referenceId: {referenceId}");
                    return Json(new { success = false, message = $"Transaction not found with reference: {referenceId}" });
                }

                return Json(new { success = true, transaction });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        private async Task<object> GetTransactionDetailsFromSP(int sellerId, string referenceId)
        {
            using (var connection = new SqlConnection(GetConnectionString()))
            {
                using (var command = new SqlCommand("sp_GetTransactionDetails", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@SellerId", sellerId);
                    command.Parameters.AddWithValue("@ReferenceId", referenceId);

                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            // Check if we actually got data (ReferenceId not null)
                            if (reader["ReferenceId"] != DBNull.Value)
                            {
                                string source = reader["Source"]?.ToString() ?? "";
                                
                                // Handle wallet transaction (no Method column)
                                if (source == "wallet_transaction")
                                {
                                    return new
                                    {
                                        referenceId = reader["ReferenceId"].ToString(),
                                        transactionDate = Convert.ToDateTime(reader["TransactionDate"]),
                                        type = reader["Type"].ToString(),
                                        method = "N/A", // Wallet transactions don't have method
                                        amount = Convert.ToDecimal(reader["Amount"]),
                                        status = reader["Status"].ToString(),
                                        source = source,
                                        additionalDetails = new { }
                                    };
                                }
                                // Handle withdrawal request (has Method column)
                                else
                                {
                                    // Parse additional details if they exist
                                    object additionalDetails = null;
                                    if (reader["AdditionalDetails"] != DBNull.Value)
                                    {
                                        string details = reader["AdditionalDetails"].ToString();
                                        var detailsDict = new Dictionary<string, string>();
                                        
                                        // Parse the CONCAT string format: "Requested: ... | Processed: ... | Account: ..."
                                        var parts = details.Split('|');
                                        foreach (var part in parts)
                                        {
                                            var keyValue = part.Split(':');
                                            if (keyValue.Length >= 2)
                                            {
                                                string key = keyValue[0].Trim();
                                                string value = string.Join(":", keyValue.Skip(1)).Trim();
                                                detailsDict[key] = value;
                                            }
                                        }
                                        additionalDetails = detailsDict;
                                    }

                                    return new
                                    {
                                        referenceId = reader["ReferenceId"].ToString(),
                                        transactionDate = Convert.ToDateTime(reader["TransactionDate"]),
                                        type = reader["Type"].ToString(),
                                        method = reader["Method"]?.ToString() ?? "N/A",
                                        amount = Convert.ToDecimal(reader["Amount"]),
                                        status = reader["Status"].ToString(),
                                        source = source,
                                        additionalDetails = additionalDetails
                                    };
                                }
                            }
                        }
                    }
                }
            }
            return null;
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
        // A small class to catch the data from your JavaScript fetch request
public class DeclineRequest
{
    public int OrderId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
[HttpPost]
public async Task<IActionResult> DeclineOrder([FromBody] DeclineRequest request)
{
    // 1. Get the Seller ID from the session
    var sellerId = HttpContext.Session.GetInt32("SellerId");
    if (sellerId == null) return Unauthorized(new { message = "Please log in." });

    // 2. Find the order using your OrderService
    var order = await _orderService.GetOrderByIdAsync(request.OrderId, sellerId.Value);

    if (order == null)
    {
        return NotFound(new { message = "Order not found." });
    }

    // --- NEW: START CLEANUP LOGIC ---
    // If there is an existing proof of shipment, delete the file from the server
    if (!string.IsNullOrEmpty(order.ProofOfShipmentUrl))
    {
        try
        {
            // Convert the relative URL (/uploads/receipts/file.jpg) back to a physical path
            string relativePath = order.ProofOfShipmentUrl.TrimStart('/');
            string fullPath = Path.Combine(_environment.WebRootPath, relativePath);

            // Check if the file actually exists on the server before trying to delete it
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
        catch (Exception ex)
        {
            // Log the error (optional) but allow the cancellation to continue
            Console.WriteLine($"Failed to delete file: {ex.Message}");
        }
    }
    // --- END CLEANUP LOGIC ---

    // 3. Update the status, the reason, and clear the image URL
    order.Status = "Cancelled";
    order.CancellationReason = request.Reason; 
    order.ProofOfShipmentUrl = null; // Ensure the DB link is removed even if file deletion fails

    // 4. Save to database using your service
    await _orderService.UpdateOrderAsync(order);

    return Ok(new { message = "Order declined successfully and files cleaned up." });
}

        private async Task<SellerDashboardViewModel> BuildSellerDashboardModelAsync(CancellationToken cancellationToken = default)
        {
            var sellerEmail = HttpContext.Session.GetString("SellerEmail");
            var sellerIdFromSession = HttpContext.Session.GetInt32("SellerId");
            var sellerNameFromSession = HttpContext.Session.GetString("SellerName");

            SellerContextInfo sellerContext;
            if (sellerIdFromSession is > 0)
            {
                sellerContext = await _sellerContextService.ResolveSellerByIdAsync(sellerIdFromSession.Value, cancellationToken);

                if (sellerContext.SellerId <= 0)
                {
                    sellerContext = new SellerContextInfo
                    {
                        SellerId = sellerIdFromSession.Value,
                        SellerName = string.IsNullOrWhiteSpace(sellerNameFromSession) ? "Seller" : sellerNameFromSession
                    };
                }
            }
            else if (!string.IsNullOrWhiteSpace(sellerEmail))
            {
                sellerContext = await _sellerContextService.ResolveSellerAsync(sellerEmail, cancellationToken);
            }
            else
            {
                sellerContext = await _sellerContextService.ResolveSellerAsync(null, cancellationToken);
            }

            if (sellerContext.SellerId > 0)
            {
                HttpContext.Session.SetInt32("SellerId", sellerContext.SellerId);
                HttpContext.Session.SetString("SellerName", string.IsNullOrWhiteSpace(sellerContext.SellerName) ? "Seller" : sellerContext.SellerName);
            }

            var performance = await _sellerPerformanceService.GetSellerPerformanceAsync(
                sellerContext.SellerId,
                DateTime.UtcNow,
                cancellationToken);
            var monthlyRevenue = await _sellerPerformanceService.GetMonthlyRevenueByYearAsync(
                sellerContext.SellerId,
                new[] { 2024, 2025, 2026 },
                cancellationToken);
            var topProducts = await _sellerPerformanceService.GetTopPerformingProductsAsync(
                sellerContext.SellerId,
                topCount: 5,
                cancellationToken: cancellationToken);

            var now = DateTime.UtcNow;
            var t1H = _sellerPerformanceService.GetTopPerformingProductsAsync(sellerContext.SellerId, topCount: 10, from: now.AddHours(-1), cancellationToken: cancellationToken);
            var t1D = _sellerPerformanceService.GetTopPerformingProductsAsync(sellerContext.SellerId, topCount: 10, from: now.AddDays(-1), cancellationToken: cancellationToken);
            var t7D = _sellerPerformanceService.GetTopPerformingProductsAsync(sellerContext.SellerId, topCount: 10, from: now.AddDays(-7), cancellationToken: cancellationToken);
            var t1M = _sellerPerformanceService.GetTopPerformingProductsAsync(sellerContext.SellerId, topCount: 10, from: now.AddDays(-30), cancellationToken: cancellationToken);
            await Task.WhenAll(t1H, t1D, t7D, t1M);
    int? currentSellerId = HttpContext.Session.GetInt32("SellerId");
    var realOrders = new List<Order>();
    if (currentSellerId != null)
    {
        realOrders = await _orderService.GetOrdersBySellerAsync(currentSellerId.Value);
    }
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
                 RecentOrders = realOrders.Take(5).ToList(),
                TopProducts = topProducts,
                TopProductsByRange = new Dictionary<string, List<TopSellingProduct>>
                {
                    ["1H"] = t1H.Result,
                    ["1D"] = t1D.Result,
                    ["7D"] = t7D.Result,
                    ["1M"] = t1M.Result,
                }
            };
        }
        
    }
}
