using System.ComponentModel.DataAnnotations;

namespace Base.Shared.DTOs
{
    public class ResetPasswordDTO
    {
        [Required, EmailAddress]
        public required string Email { get; set; }
        [Required]
        public required string Token { get; set; }
        [Required, MinLength(8)]
        //[RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
        //ErrorMessage = "Password must be at least 8 characters and contain uppercase, lowercase, number, and special character.")]
        public required string NewPassword { get; set; }
    }
}
