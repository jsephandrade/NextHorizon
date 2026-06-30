using System.Threading.Tasks;
using NextHorizon.Models;

namespace NextHorizon.Services
{
    public interface IAuthService
    {
        Task<LoginResponseModel> AuthenticateAsync(LoginRequestModel request);
        Task<bool> LogoutAsync(int staffId);
        Task<bool> RequestPasswordResetAsync(string email);
        Task<bool> ValidateResetTokenAsync(string token);
        Task<bool> ResetPasswordAsync(string token, string newPassword);
        Task<AuthenticatedUser> GetAuthenticatedUserAsync(int staffId);
    }
}