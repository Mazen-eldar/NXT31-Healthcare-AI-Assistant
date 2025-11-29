using System.ComponentModel.DataAnnotations;

namespace Base.Shared.DTOs
{
    public class ForgotPasswordDTO
    {
        [Required]
        public required string Email { get; set; }

    }
}
