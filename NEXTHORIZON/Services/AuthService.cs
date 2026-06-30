    using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NextHorizon.Models;

namespace NextHorizon.Services
{
    public class AuthService : IAuthService
    {
        private readonly string _connectionString;

        public AuthService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<LoginResponseModel> AuthenticateAsync(LoginRequestModel request)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("sp_AuthenticateAdmin", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@Username", request.Username);
                        command.Parameters.AddWithValue("@Password", request.Password);
                        command.Parameters.AddWithValue("@SelectedRole", request.SelectedRole);

                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var status = reader["Status"].ToString();
                                
                                if (status == "Success")
                                {
                                    return new LoginResponseModel
                                    {
                                        Success = true,
                                        Message = reader["Message"].ToString(),
                                        UserType = reader["UserType"].ToString(),
                                        RedirectUrl = GetRedirectUrl(reader["UserType"].ToString())
                                    };
                                }
                                else
                                {
                                    return new LoginResponseModel
                                    {
                                        Success = false,
                                        Message = reader["Message"].ToString()
                                    };
                                }
                            }
                        }
                    }
                }
                
                return new LoginResponseModel
                {
                    Success = false,
                    Message = "Invalid username or password"
                };
            }
            catch (Exception ex)
            {
                return new LoginResponseModel
                {
                    Success = false,
                    Message = "An error occurred during authentication"
                };
            }
        }

        public async Task<AuthenticatedUser> GetAuthenticatedUserAsync(int staffId)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand(@"
                        SELECT 
                            s.staff_id,
                            s.user_id,
                            s.username,
                            s.first_name,
                            s.last_name,
                            u.email,
                            u.user_type,
                            s.permissions,
                            s.last_active,
                            s.IsActive
                        FROM staff_info s
                        INNER JOIN users u ON s.user_id = u.user_id
                        WHERE s.staff_id = @StaffId AND s.IsActive = 1", connection))
                    {
                        command.Parameters.AddWithValue("@StaffId", staffId);
                        
                        await connection.OpenAsync();
                        
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new AuthenticatedUser
                                {
                                    StaffId = reader.GetInt32(0),
                                    UserId = reader.GetInt32(1),
                                    Username = reader.GetString(2),
                                    FirstName = reader.GetString(3),
                                    LastName = reader.GetString(4),
                                    Email = reader.GetString(5),
                                    UserType = reader.GetString(6),
                                    Permissions = reader.IsDBNull(7) ? null : reader.GetString(7),
                                    LastActive = reader.IsDBNull(8) ? DateTime.Now : reader.GetDateTime(8)
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error
            }
            
            return null;
        }

        public Task<bool> LogoutAsync(int staffId)
        {
            // Update last_active or perform any cleanup
            return Task.FromResult(true);
        }

        public Task<bool> RequestPasswordResetAsync(string email)
        {
            // Implement password reset logic
            return Task.FromResult(true);
        }

        public Task<bool> ValidateResetTokenAsync(string token)
        {
            // Implement token validation
            return Task.FromResult(true);
        }

        public Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            // Implement password reset
            return Task.FromResult(true);
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
