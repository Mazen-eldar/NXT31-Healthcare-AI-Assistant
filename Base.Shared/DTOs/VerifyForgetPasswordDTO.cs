using System.ComponentModel.DataAnnotations;

namespace Base.Shared.DTOs
{
    public class VerifyForgetPasswordDTO
    {
        [Required]
        public required string Email { get; set; }
        [Required]
        public required string Otp { get; set; }
        //[Required]
        //public required string NewPassword { get; set; }
    }
}
