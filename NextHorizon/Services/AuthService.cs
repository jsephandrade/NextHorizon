using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using NextHorizon.Models;

namespace NextHorizon.Services;

public sealed class AuthService : IAuthService
{
    private readonly string _connectionString;
    private readonly PasswordHasher<object> _passwordHasher = new();

    public AuthService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
    }

    public async Task<LoginResponseModel> AuthenticateAsync(LoginRequestModel request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new LoginResponseModel
            {
                Success = false,
                Message = "Username and password are required."
            };
        }

        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("sp_AuthenticateAdmin", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@Username", request.Username.Trim());
            command.Parameters.AddWithValue("@Password", request.Password);

            await connection.OpenAsync();
            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return new LoginResponseModel
                {
                    Success = false,
                    Message = "No data returned from database."
                };
            }

            var status = reader["Status"]?.ToString();
            if (!string.Equals(status, "Success", StringComparison.OrdinalIgnoreCase))
            {
                return new LoginResponseModel
                {
                    Success = false,
                    Message = reader["Message"]?.ToString() ?? "Invalid username or password."
                };
            }

            var storedHash = reader["PasswordHash"]?.ToString();
            if (string.IsNullOrWhiteSpace(storedHash))
            {
                return new LoginResponseModel
                {
                    Success = false,
                    Message = "Password verification data is missing."
                };
            }

            var verificationResult = _passwordHasher.VerifyHashedPassword(null!, storedHash, request.Password);
            if (verificationResult is not PasswordVerificationResult.Success and not PasswordVerificationResult.SuccessRehashNeeded)
            {
                return new LoginResponseModel
                {
                    Success = false,
                    Message = "Invalid username or password."
                };
            }

            var user = new AuthenticatedUser
            {
                StaffId = SafeInt(reader["StaffId"]),
                UserId = SafeInt(reader["UserId"]),
                Username = reader["Username"]?.ToString() ?? request.Username.Trim(),
                FullName = reader["FullName"]?.ToString() ?? string.Empty,
                Email = reader["Email"]?.ToString() ?? string.Empty,
                UserType = reader["UserType"]?.ToString() ?? string.Empty,
                LastActive = DateTime.UtcNow
            };

            if (!string.IsNullOrWhiteSpace(user.FullName))
            {
                var nameParts = user.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                user.FirstName = nameParts.FirstOrDefault() ?? string.Empty;
                user.LastName = nameParts.Length > 1 ? string.Join(' ', nameParts.Skip(1)) : string.Empty;
            }

            return new LoginResponseModel
            {
                Success = true,
                Message = "Login successful",
                UserType = user.UserType,
                RedirectUrl = "/qa/dashboard",
                User = user
            };
        }
        catch
        {
            return new LoginResponseModel
            {
                Success = false,
                Message = "An error occurred during authentication."
            };
        }
    }

    public async Task<AuthenticatedUser?> GetAuthenticatedUserAsync(int staffId)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            const string query = """
                SELECT
                    s.staff_id,
                    s.user_id,
                    s.username,
                    s.first_name,
                    s.last_name,
                    u.email,
                    u.user_type,
                    s.permissions,
                    s.last_active
                FROM staff_info s
                INNER JOIN users u ON s.user_id = u.user_id
                WHERE s.staff_id = @StaffId AND s.IsActive = 1
                """;

            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@StaffId", staffId);

            await connection.OpenAsync();
            await using var reader = await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return null;
            }

            var firstName = reader["first_name"]?.ToString() ?? string.Empty;
            var lastName = reader["last_name"]?.ToString() ?? string.Empty;

            return new AuthenticatedUser
            {
                StaffId = SafeInt(reader["staff_id"]),
                UserId = SafeInt(reader["user_id"]),
                Username = reader["username"]?.ToString() ?? string.Empty,
                FirstName = firstName,
                LastName = lastName,
                FullName = string.Join(' ', new[] { firstName, lastName }.Where(part => !string.IsNullOrWhiteSpace(part))),
                Email = reader["email"]?.ToString() ?? string.Empty,
                UserType = reader["user_type"]?.ToString() ?? string.Empty,
                Permissions = reader["permissions"] == DBNull.Value ? string.Empty : reader["permissions"]?.ToString() ?? string.Empty,
                LastActive = reader["last_active"] == DBNull.Value
                    ? DateTime.UtcNow
                    : Convert.ToDateTime(reader["last_active"])
            };
        }
        catch
        {
            return null;
        }
    }

    public Task<bool> LogoutAsync(int staffId)
    {
        return Task.FromResult(true);
    }

    public Task<bool> RequestPasswordResetAsync(string email)
    {
        return Task.FromResult(false);
    }

    public Task<bool> ValidateResetTokenAsync(string token)
    {
        return Task.FromResult(false);
    }

    public Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        return Task.FromResult(false);
    }

    private static int SafeInt(object value)
    {
        return value == DBNull.Value ? 0 : Convert.ToInt32(value);
    }
}
