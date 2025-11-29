using Microsoft.AspNetCore.Http;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Base.Shared.DTOs

{
    public class RegisterDTO
    {
        [Required, EmailAddress]
        public required string Email { get; set; }

        [Required, MinLength(8)]
        //[RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
        //    ErrorMessage = "Password must contain at least 8 characters, one uppercase, one lowercase, one number and one special character.")]
        public required string Password { get; set; }

        [Required, MinLength(2)]
        public required string FullName { get; set; }

        //[Required]
        //public required string UserType { get; set; }

        [Required]
        [Phone]
        public string? PhoneNumber { get; set; }
    }
    public enum UserTypes
    {
        ClinicDoctor,
        ClinicReceptionist,
        ClinicAdmin,
        SystemAdmin,
        User
    }
}
