using System.ComponentModel.DataAnnotations;

namespace NextHorizon.ViewModels
{
    public class UpdateProfileViewModel
    {
        public int UserId { get; set; }
        
        [Display(Name = "First Name")]
        public string? FirstName { get; set; }
        
        [Display(Name = "Middle Name")]
        public string? MiddleName { get; set; }
        
        [Display(Name = "Last Name")]
        public string? LastName { get; set; }
        
        [Display(Name = "Full Name")]
        public string? FullName { get; set; }
        
        [Display(Name = "Username")]
        public string? Username { get; set; }
        
        [Display(Name = "Address")]
        public string? Address { get; set; }
        
        [Display(Name = "Phone Number")]
        [Phone]
        public string? PhoneNumber { get; set; }
        
        [Display(Name = "Email")]
        [EmailAddress]
        public string? Email { get; set; }
    }

    public class UpdateFieldViewModel
    {
        [Required]
        public string Field { get; set; } = string.Empty;
        
        [Required]
        public string Value { get; set; } = string.Empty;
    }

    public class UpdateNameViewModel
    {
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
    }

    public class UpdatePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}