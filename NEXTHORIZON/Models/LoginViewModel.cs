using System.ComponentModel.DataAnnotations;

namespace NextHorizon.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [Display(Name = "Username")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Selected Role")]
        public string SelectedRole { get; set; }
        
        public string AccessLevel { get; set; }
        
        public bool RememberMe { get; set; }
    }

    public class LoginRequestModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string SelectedRole { get; set; }
    }

    public class LoginResponseModel
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string UserType { get; set; }
        public string RedirectUrl { get; set; }
        public string Token { get; set; }
    }

    public class ResetPasswordRequestModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }
    }
}