using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using System.Data;
using System.Threading.Tasks;
using System;
using NextHorizon.Models;
using NextHorizon.Services;
using System.Text.Json;

namespace NextHorizon.Controllers
{
    public class LoginController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly string _connectionString;
        private readonly PasswordHasher<object> _passwordHasher;

        public LoginController(IConfiguration configuration, IEmailService emailService)
        {
            _configuration = configuration;
            _emailService = emailService;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
            _passwordHasher = new PasswordHasher<object>();
        }

        public IActionResult AdminLogin()
        {
            HttpContext.Session.Clear();
            return View(new LoginViewModel());
        }

        [HttpGet]
        public async Task<IActionResult> GetUserType(string username)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var command = new SqlCommand(@"
                        SELECT u.user_type 
                        FROM users u
                        LEFT JOIN staff_info s ON u.user_id = s.user_id
                        WHERE s.username = @Username", connection);
                    
                    command.Parameters.AddWithValue("@Username", username);
                    
                    await connection.OpenAsync();
                    var userType = await command.ExecuteScalarAsync() as string;
                    
                    if (userType != null)
                    {
                        return Json(new { success = true, userType = userType });
                    }
                    else
                    {
                        return Json(new { success = false, message = "User not found" });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AuthenticateAdmin([FromBody] LoginRequestModel request)
{
            try
            {
                Console.WriteLine($"=== Login Attempt for user: {request.Username} ===");
                
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_AuthenticateAdmin", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@Username", request.Username);
                        command.Parameters.AddWithValue("@Password", request.Password);

                        await connection.OpenAsync();
                        Console.WriteLine("Database connection opened");
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var status = reader["Status"].ToString();
                                
                                if (status == "Success")
                                {
                                    var storedHash = reader["PasswordHash"].ToString();
                                    
                                    // Verify in C# with PasswordHasher
                                    var verificationResult = _passwordHasher.VerifyHashedPassword(null, storedHash, request.Password);
                                    
                                    if (verificationResult == PasswordVerificationResult.Success || 
                                        verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
                                    {
                                        string userType = reader["UserType"].ToString();
                                        
                                        // Set session
                                        HttpContext.Session.SetInt32("StaffId", Convert.ToInt32(reader["StaffId"]));
                                        HttpContext.Session.SetInt32("UserId", Convert.ToInt32(reader["UserId"]));
                                        HttpContext.Session.SetString("Username", reader["Username"].ToString());
                                        HttpContext.Session.SetString("FullName", reader["FullName"].ToString());
                                        HttpContext.Session.SetString("Email", reader["Email"].ToString()); 
                                        HttpContext.Session.SetString("UserType", userType);

                                        await LogAdminAction(
                                            Convert.ToInt32(reader["StaffId"]),
                                            reader["Username"].ToString(),
                                            "Login",
                                            "System",
                                            "Success",
                                            $"Successful login as {userType}"
                                        );

                                        return Json(new LoginResponseModel
                                        {
                                            Success = true,
                                            Message = "Login successful",
                                            UserType = userType,
                                            RedirectUrl = GetRedirectUrl(userType)
                                        });
                                    }
                                    else
                                    {
                                        return Json(new LoginResponseModel
                                        {
                                            Success = false,
                                            Message = "Invalid username or password"
                                        });
                                    }
                                }
                                else
                                {
                                    return Json(new LoginResponseModel
                                    {
                                        Success = false,
                                        Message = reader["Message"].ToString()
                                    });
                                }
                            }
                            else
                            {
                                return Json(new LoginResponseModel
                                {
                                    Success = false,
                                    Message = "No data returned from database"
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return Json(new LoginResponseModel
                {
                    Success = false,
                    Message = "An error occurred during login"
                });
            }
        }
        
        [HttpPost]
        public async Task<IActionResult> GenerateOTP([FromBody] ForgotPasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Invalid email format" });
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_GeneratePasswordOTP", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@Email", model.Email);

                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var message = reader["Message"].ToString();
                                
                                if (reader["OTPCode"] != DBNull.Value)
                                {
                                    var otpCode = reader["OTPCode"].ToString();
                                    var userName = reader["UserName"].ToString();
                                    var userEmail = reader["UserEmail"].ToString();

                                    var emailSent = await _emailService.SendOTPEmailAsync(userEmail, userName, otpCode);

                                    if (emailSent)
                                    {
                                        return Json(new { 
                                            success = true, 
                                            message = "OTP sent to your email address",
                                            email = userEmail
                                        });
                                    }
                                    else
                                    {
                                        return Json(new { 
                                            success = false, 
                                            message = "Failed to send email. Please try again." 
                                        });
                                    }
                                }
                                else
                                {
                                    return Json(new { 
                                        success = true, 
                                        message = "If your email exists in our system, an OTP will be sent."
                                    });
                                }
                            }
                        }
                    }
                }
                
                return Json(new { success = true, message = "If your email exists in our system, an OTP will be sent." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred. Please try again." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> VerifyOTP([FromBody] VerifyOTPModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new VerifyOTPResponseModel 
                { 
                    Status = "Error", 
                    Message = "Invalid input" 
                });
            }

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_VerifyOTP", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@Email", model.Email);
                        command.Parameters.AddWithValue("@OtpCode", model.OTP);

                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return Json(new VerifyOTPResponseModel
                                {
                                    Status = reader["Status"].ToString(),
                                    Message = reader["Message"].ToString(),
                                    ResetToken = reader["ResetToken"] != DBNull.Value ? 
                                        Guid.Parse(reader["ResetToken"].ToString()) : (Guid?)null
                                });
                            }
                        }
                    }
                }
                
                return Json(new VerifyOTPResponseModel
                {
                    Status = "Error",
                    Message = "Invalid OTP code"
                });
            }
            catch (Exception ex)
            {
                return Json(new VerifyOTPResponseModel
                {
                    Status = "Error",
                    Message = "An error occurred. Please try again."
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(new ResetPasswordResponseModel
            {
                Status = "Error",
                Message = "Please correct all errors"
            });
        }

        try
        {
            // Hash the new password using PasswordHasher (ASP.NET Identity format)
            var hashedPassword = _passwordHasher.HashPassword(null, model.NewPassword);

            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand("sp_ResetPasswordWithOTP", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@Email", model.Email);
                    command.Parameters.AddWithValue("@ResetToken", model.ResetToken);
                    command.Parameters.AddWithValue("@NewPassword", hashedPassword);

                    await connection.OpenAsync();
                    
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            var status = reader["Status"].ToString();
                            var message = reader["Message"].ToString();

                            if (status == "Success")
                            {
                                // Send confirmation email
                                _ = Task.Run(async () => {
                                    await _emailService.SendPasswordResetConfirmationAsync(model.Email, "User");
                                });
                            }

                            return Json(new ResetPasswordResponseModel
                            {
                                Status = status,
                                Message = message
                            });
                        }
                    }
                }
            }
            
            return Json(new ResetPasswordResponseModel
            {
                Status = "Error",
                Message = "Password reset failed"
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ResetPassword Error: {ex.Message}");
            return Json(new ResetPasswordResponseModel
            {
                Status = "Error",
                Message = "An error occurred. Please try again."
            });
        }
    }
        public async Task<IActionResult> Logout()
        {
            var staffId = HttpContext.Session.GetInt32("StaffId");
            
            if (staffId.HasValue)
            {
                await LogAdminAction(staffId.Value, "Logout", "System", "Success", "User logged out");
            }
            
            HttpContext.Session.Clear();
            return RedirectToAction("AdminLogin");
        }

        private async Task LogAdminAction(int staffId, string action, string target, string status, string details = null)
        {
            await LogAdminAction(staffId, null, action, target, status, details);
        }

        private async Task LogAdminAction(int staffId, string adminName, string action, string target, string status, string details = null)
        {
            try
            {
                if (string.IsNullOrEmpty(adminName))
                {
                    adminName = HttpContext.Session.GetString("Username") ?? "System";
                }

                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_InsertAuditLog", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@StaffId", staffId);
                        command.Parameters.AddWithValue("@AdminName", adminName);
                        command.Parameters.AddWithValue("@Action", action);
                        command.Parameters.AddWithValue("@Target", target);
                        command.Parameters.AddWithValue("@TargetType", "System");
                        command.Parameters.AddWithValue("@Status", status);
                        command.Parameters.AddWithValue("@Details", details ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@IpAddress", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown");
                        command.Parameters.AddWithValue("@UserAgent", Request.Headers["User-Agent"].ToString());

                        await connection.OpenAsync();
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to log admin action: {ex.Message}");
            }
        }

        private string GetRedirectUrl(string userType)
        {
            var normalizedRole = (userType ?? string.Empty).Trim();

            return normalizedRole switch
            {
                "SuperAdmin" => "/Admin/Dashboard",
                "Admin" => "/Admin/Dashboard",
                "Finance Officer" => "/Admin/FinanceRequest",
                "Support Agent" => "/Agent/AgentDashboard",
                "QA" => "/qa/dashboard",
                "QA Analyst" => "/qa/dashboard",
                "QA Head" => "/qa/dashboard",
                "QA Reviewer" => "/qa/dashboard",
                _ => "/Agent/HelpCenter"
            };
        }
    }
}
