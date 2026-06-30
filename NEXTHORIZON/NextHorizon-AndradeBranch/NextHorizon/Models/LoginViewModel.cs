using System.ComponentModel.DataAnnotations;

namespace NextHorizon.Models;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "Username is required")]
    [Display(Name = "Username")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Selected Role")]
    public string? SelectedRole { get; set; }

    public string AccessLevel { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}

public sealed class LoginRequestModel
{
    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string? SelectedRole { get; set; }
}

public sealed class LoginResponseModel
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public string UserType { get; set; } = string.Empty;

    public string RedirectUrl { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;

    public AuthenticatedUser? User { get; set; }
}

public sealed class ResetPasswordRequestModel
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;
}

public sealed class ForgotPasswordModel
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [Display(Name = "Work Email")]
    public string Email { get; set; } = string.Empty;
}

public sealed class VerifyOTPModel
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "OTP code is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP must be 6 digits")]
    public string OTP { get; set; } = string.Empty;
}

public sealed class ResetPasswordModel
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Reset token is required")]
    public Guid ResetToken { get; set; }

    [Required(ErrorMessage = "New password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public sealed class OTPResponseModel
{
    public string Message { get; set; } = string.Empty;

    public string OTPCode { get; set; } = string.Empty;

    public string UserEmail { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;
}

public sealed class VerifyOTPResponseModel
{
    public string Status { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public Guid? ResetToken { get; set; }
}

public sealed class ResetPasswordResponseModel
{
    public string Status { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}

public sealed class AuthenticatedUser
{
    public int StaffId { get; set; }

    public int UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string UserType { get; set; } = string.Empty;

    public string AccessLevel { get; set; } = string.Empty;

    public string Permissions { get; set; } = string.Empty;

    public DateTime LastActive { get; set; }

    public bool IsSuperAdmin => UserType == "SuperAdmin";

    public bool IsAdmin => UserType == "Admin" || UserType == "SuperAdmin";

    public bool IsFinanceOfficer => UserType == "Finance Officer";

    public bool IsSupportAgent => UserType == "Support Agent";
}
