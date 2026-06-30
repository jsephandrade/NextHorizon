using System;
using System.ComponentModel.DataAnnotations;

namespace NextHorizon.Models
{
    public class ForgotPasswordModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Work Email")]
        public string Email { get; set; }
    }

    public class VerifyOTPModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "OTP code is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP must be 6 digits")]
        public string OTP { get; set; }
    }

    public class ResetPasswordModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Reset token is required")]
        public Guid ResetToken { get; set; }

        [Required(ErrorMessage = "New password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Confirm password is required")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; }
    }

    public class OTPResponseModel
    {
        public string Message { get; set; }
        public string OTPCode { get; set; }
        public string UserEmail { get; set; }
        public string UserName { get; set; }
    }

    public class VerifyOTPResponseModel
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public Guid? ResetToken { get; set; }
    }

    public class ResetPasswordResponseModel
    {
        public string Status { get; set; }
        public string Message { get; set; }
    }
}