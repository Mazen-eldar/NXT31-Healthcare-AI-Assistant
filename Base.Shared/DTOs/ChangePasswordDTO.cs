using System.ComponentModel.DataAnnotations;

namespace Base.Shared.DTOs
{
    /*public class ChangePasswordDTO
    {
        [Required]
        public required string CurrentPassword { get; set; }
        [Required]
        public required string NewPassword { get; set; }

    }*/

    public class ChangePasswordDTO
    {
        [Required(ErrorMessage = "Current password is required.")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
        //[RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$",
        //    ErrorMessage = "Password must contain uppercase, lowercase, digit, and special character.")]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        [DataType(DataType.Password)]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
