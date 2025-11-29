using System.ComponentModel.DataAnnotations;

namespace Base.Shared.DTOs
{
    public class VerifyOtpDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(6, MinimumLength = 6)] // Assuming OTP is 6 digits
        [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP must be 6 digits.")]
        public string Otp { get; set; }
    }
}
