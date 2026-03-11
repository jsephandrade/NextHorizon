using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.ViewModels
{
public class BecomeSellerViewModel
{
    // Business Type
    [Required(ErrorMessage = "Business type is required")]
    public string BusinessType { get; set; }

    // Files
    public IFormFile BrandLogo { get; set; }

    [Required(ErrorMessage = "DTI certificate is required")]
    public IFormFile DtiCertificate { get; set; }

    [Required(ErrorMessage = "BIR certificate is required")]
    public IFormFile BirCertificate { get; set; }

    [Required(ErrorMessage = "Business permit is required")]
    public IFormFile BusinessPermit { get; set; }

    public IFormFile AdditionalDocument { get; set; }

    // Business Information
   [Required(ErrorMessage = "Business Name is required")]
        [Display(Name = "Business Name")]
    public string BusinessName { get; set; }

    [Required(ErrorMessage = "Business Email is required")]
        [Display(Name = "Business Email")]
    [EmailAddress]
    public string BusinessEmail { get; set; }

    [Required(ErrorMessage = "Business Phone is required")]
        [Display(Name = "Business Phone")]
    public string BusinessPhone { get; set; }

   [Required(ErrorMessage = "Tax ID is required")]
        [Display(Name = "Tax ID")]
    public string TaxId { get; set; }

  [Required(ErrorMessage = "Business Address is required")]
        [Display(Name = "Business Address")]
    public string BusinessAddress { get; set; }

    // Login credentials (for seller login)
   [Required(ErrorMessage = "First Name is required")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        public string? MiddleName { get; set; }

        [Required(ErrorMessage = "Last Name is required")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Phone Number is required")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

[Required(ErrorMessage = "Username is required")]
    public string Username { get; set; }

  [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
     [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
    public string Password { get; set; }

  [Required(ErrorMessage = "Confirm Password is required")]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; }

    [Required]
    public bool AgreeTerms { get; set; }
}
}