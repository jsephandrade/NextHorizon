using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using NextHorizon.Models;
using NextHorizon.Services;

namespace NextHorizon.Controllers;

public sealed class LoginController : Controller
{
    private static readonly string[] AllowedWorkspaceRoles = ["QA Analyst", "Support Agent"];
    private const string AllowedWorkspaceRolesLabel = "QA Analyst and Support Agent";

    private readonly IAuthService _authService;
    private readonly IEmailService _emailService;
    private readonly string _connectionString;
    private readonly PasswordHasher<object> _passwordHasher = new();

    public LoginController(IConfiguration configuration, IAuthService authService, IEmailService emailService)
    {
        _authService = authService;
        _emailService = emailService;
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
    }

    [HttpGet]
    public IActionResult AdminLogin()
    {
        HttpContext.Session.Clear();
        return View(new LoginViewModel());
    }

    [HttpGet]
    public async Task<IActionResult> GetUserType(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return Json(new { success = false, message = "Username is required." });
        }

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            const string query = """
                SELECT u.user_type
                FROM users u
                LEFT JOIN staff_info s ON u.user_id = s.user_id
                WHERE s.username = @Username
                """;

            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Username", username.Trim());

            await connection.OpenAsync();
            var userType = await command.ExecuteScalarAsync() as string;

            return string.IsNullOrWhiteSpace(userType)
                ? Json(new { success = false, message = "User not found." })
                : Json(new { success = true, userType });
        }
        catch
        {
            return Json(new { success = false, message = "Unable to retrieve user type." });
        }
    }

    [HttpPost]
    public async Task<IActionResult> AuthenticateAdmin([FromBody] LoginRequestModel request)
    {
        var response = await _authService.AuthenticateAsync(request);
        if (!response.Success || response.User is null)
        {
            return Json(response);
        }

        if (!IsAllowedWorkspaceUser(response.User.UserType))
        {
            HttpContext.Session.Clear();
            return Json(new LoginResponseModel
            {
                Success = false,
                Message = $"Workspace access is restricted to {AllowedWorkspaceRolesLabel} users only."
            });
        }

        HttpContext.Session.SetInt32("StaffId", response.User.StaffId);
        HttpContext.Session.SetInt32("UserId", response.User.UserId);
        HttpContext.Session.SetString("Username", response.User.Username);
        HttpContext.Session.SetString("FullName", response.User.FullName);
        HttpContext.Session.SetString("Email", response.User.Email);
        HttpContext.Session.SetString("UserType", response.User.UserType);

        await LogAdminAction(
            response.User.StaffId,
            response.User.Username,
            "Login",
            response.RedirectUrl,
            "Success",
            $"Successful workspace login as {response.User.UserType}");

        return Json(response);
    }

    [HttpPost]
    public async Task<IActionResult> GenerateOTP([FromBody] ForgotPasswordModel model)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Invalid email format." });
        }

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("sp_GeneratePasswordOTP", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@Email", model.Email);

            await connection.OpenAsync();
            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                if (reader["OTPCode"] != DBNull.Value)
                {
                    var otpCode = reader["OTPCode"]?.ToString() ?? string.Empty;
                    var userName = reader["UserName"]?.ToString() ?? "QA User";
                    var userEmail = reader["UserEmail"]?.ToString() ?? model.Email;
                    var emailSent = await _emailService.SendOTPEmailAsync(userEmail, userName, otpCode);

                    return emailSent
                        ? Json(new { success = true, message = "OTP sent to your email address.", email = userEmail })
                        : Json(new { success = false, message = "Email sending is not configured or failed." });
                }

                return Json(new
                {
                    success = true,
                    message = "If your email exists in our system, an OTP will be sent."
                });
            }

            return Json(new
            {
                success = true,
                message = "If your email exists in our system, an OTP will be sent."
            });
        }
        catch
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
                Message = "Invalid input."
            });
        }

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("sp_VerifyOTP", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@Email", model.Email);
            command.Parameters.AddWithValue("@OtpCode", model.OTP);

            await connection.OpenAsync();
            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                Guid? resetToken = null;
                if (reader["ResetToken"] != DBNull.Value && Guid.TryParse(reader["ResetToken"]?.ToString(), out var parsedToken))
                {
                    resetToken = parsedToken;
                }

                return Json(new VerifyOTPResponseModel
                {
                    Status = reader["Status"]?.ToString() ?? "Error",
                    Message = reader["Message"]?.ToString() ?? "Invalid OTP code.",
                    ResetToken = resetToken
                });
            }

            return Json(new VerifyOTPResponseModel
            {
                Status = "Error",
                Message = "Invalid OTP code."
            });
        }
        catch
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
                Message = "Please correct all errors."
            });
        }

        try
        {
            var hashedPassword = _passwordHasher.HashPassword(null!, model.NewPassword);

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("sp_ResetPasswordWithOTP", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@Email", model.Email);
            command.Parameters.AddWithValue("@ResetToken", model.ResetToken);
            command.Parameters.AddWithValue("@NewPassword", hashedPassword);

            await connection.OpenAsync();
            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                var status = reader["Status"]?.ToString() ?? "Error";
                var message = reader["Message"]?.ToString() ?? "Password reset failed.";

                if (string.Equals(status, "Success", StringComparison.OrdinalIgnoreCase))
                {
                    var name = model.Email.Split('@')[0];
                    _ = Task.Run(() => _emailService.SendPasswordResetConfirmationAsync(model.Email, name));
                }

                return Json(new ResetPasswordResponseModel
                {
                    Status = status,
                    Message = message
                });
            }

            return Json(new ResetPasswordResponseModel
            {
                Status = "Error",
                Message = "Password reset failed."
            });
        }
        catch
        {
            return Json(new ResetPasswordResponseModel
            {
                Status = "Error",
                Message = "An error occurred. Please try again."
            });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Logout()
    {
        var staffId = HttpContext.Session.GetInt32("StaffId");
        if (staffId.HasValue)
        {
            await LogAdminAction(staffId.Value, "Logout", "QA Workspace", "Success", "User logged out");
            await _authService.LogoutAsync(staffId.Value);
        }

        HttpContext.Session.Clear();
        return RedirectToAction(nameof(AdminLogin));
    }

    private Task LogAdminAction(int staffId, string action, string target, string status, string? details = null)
    {
        return LogAdminAction(staffId, null, action, target, status, details);
    }

    private static bool IsAllowedWorkspaceUser(string? userType)
    {
        return AllowedWorkspaceRoles.Contains(userType?.Trim() ?? string.Empty, StringComparer.OrdinalIgnoreCase);
    }

    private async Task LogAdminAction(int staffId, string? adminName, string action, string target, string status, string? details = null)
    {
        try
        {
            adminName ??= HttpContext.Session.GetString("Username") ?? "System";

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("sp_InsertAuditLog", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@StaffId", staffId);
            command.Parameters.AddWithValue("@AdminName", adminName);
            command.Parameters.AddWithValue("@Action", action);
            command.Parameters.AddWithValue("@Target", target);
            command.Parameters.AddWithValue("@TargetType", "System");
            command.Parameters.AddWithValue("@Status", status);
            command.Parameters.AddWithValue("@Details", details ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@IpAddress", HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown");
            command.Parameters.AddWithValue("@UserAgent", Request.Headers.UserAgent.ToString());

            await connection.OpenAsync();
            await command.ExecuteNonQueryAsync();
        }
        catch
        {
            // Audit logging should not block login/logout flows.
        }
    }
}
