using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace WebApplication1.ViewModels
{
    public class VerifyOtpViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; }


        [Required(ErrorMessage = "OTP is required")]
        [Range(100000, 999999, ErrorMessage = "OTP must be 6 digits")]
        public int Otp { get; set; }
    }
}