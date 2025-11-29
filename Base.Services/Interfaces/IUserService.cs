using Base.DAL.Models.BaseModels;
using Base.DAL.Models.SystemModels;
using Base.Repo.Interfaces;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Services.Interfaces
{
    public interface IUserService
    {
        Task<ApplicationUser> GetByEmailAsync(string email);
        Task<ApplicationUser> GetByIdAsync(string userId);
        Task<bool> CheckPasswordAsync(ApplicationUser user, string password);
        Task<IList<string>> GetRolesAsync(ApplicationUser user);
        Task<bool> IsLockedOutAsync(ApplicationUser user);
        //Task<ApplicationUser> GetOrCreateExternalUserAsync(ApplicationUser? user, string Password);
        Task<bool> CreateUserAsync(ApplicationUser? _user, string Password);
        Task<bool> UpdateUserAsync(ApplicationUser? user);
        Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user);
        Task<bool> ResetPasswordAsync(ApplicationUser user, string token, string newPassword);
        Task<IdentityResult> ChangePasswordAsync(ApplicationUser user, string currentPassword, string newPassword);
        Task AddAccessTokenToBlackListFromHeaderAsync();
    }
    

}
