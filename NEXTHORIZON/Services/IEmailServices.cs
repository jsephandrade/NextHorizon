using System.Threading.Tasks;

namespace NextHorizon.Services
{
    public interface IEmailService
    {
        Task<bool> SendOTPEmailAsync(string email, string name, string otpCode);
        Task<bool> SendPasswordResetConfirmationAsync(string email, string name);
        Task<bool> SendAccountDeletionEmailAsync(string email, string name, string adminName);
        Task<bool> SendAccountRestoreEmailAsync(string email, string name, string adminName);
        Task<bool> SendSellerStatusUpdateEmailAsync(string email, string businessName, string status, string note, string adminName);
        Task<bool> SendAdminCredentialsEmailAsync(string email, string firstName, string lastName, string username, string password, string userType, string addedByAdmin);
         Task<bool> SendAdminRevokedEmailAsync(string email, string firstName, string lastName, string userType, string reason, string revokedByAdmin);
    }
}