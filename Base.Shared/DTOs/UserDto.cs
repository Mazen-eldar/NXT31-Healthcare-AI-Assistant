using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Shared.DTOs
{
    public class UserDto
    {
        public string? Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; } = string.Empty;
        //public string UserType { get; set; } = string.Empty; // Admin, Farmer, Technician, etc.
        public UserTypes UserType { get; set; }
        public bool IsActive { get; set; }
        public string? ImagePath { get; set; }
        public DateTime DateOfCreation { get; set; }
        public int FarmsCount { get; set; }
    }

    // DTOs/Users/UserListDto.cs
    public class UserListDto
    {
        public List<UserDto> Users { get; set; } = new();
        public int TotalCount { get; set; }
        public int FilteredCount { get; set; }
    }

    // DTOs/Users/CreateUserRequest.cs
    public class CreateUserRequest
    {
        [Required]
        public required string FullName { get; set; } = string.Empty;
        [Required]
        public required string Email { get; set; } = string.Empty;
        [Required]
        public required string Password { get; set; } = string.Empty;
        public UserTypes UserType { get; set; } = UserTypes.SystemAdmin; // default
        public string? PhoneNumber { get; set; }
    }

    // DTOs/Users/UpdateUserRequest.cs
    public class UpdateUserRequest
    {
        public string? FullName { get; set; }
        public UserTypes? UserType { get; set; }
        public bool? IsActive { get; set; }
        public string? ImagePath { get; set; }
    }

    
}
