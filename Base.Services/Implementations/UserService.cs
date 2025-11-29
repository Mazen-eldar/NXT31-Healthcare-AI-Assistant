using Base.DAL.Models.BaseModels;
using Base.DAL.Models.SystemModels;
using Base.Repo.Interfaces;
using Base.Services.Interfaces;
using Base.Shared.DTOs;
using Base.Shared.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserService(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, IUnitOfWork unitOfWork,IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<ApplicationUser> GetByEmailAsync(string email)
            => await _userManager.FindByEmailAsync(email);

        public async Task<ApplicationUser> GetByIdAsync(string userId)
            => await _userManager.FindByIdAsync(userId);

        public async Task<bool> CheckPasswordAsync(ApplicationUser user, string password)
            => await _userManager.CheckPasswordAsync(user, password);

        public async Task<IList<string>> GetRolesAsync(ApplicationUser user)
            => await _userManager.GetRolesAsync(user);

        public async Task<bool> IsLockedOutAsync(ApplicationUser user)
            => await _userManager.IsLockedOutAsync(user);

        public async Task<bool> CreateUserAsync(ApplicationUser? _user, string Password)
        {
            /*var _user = await GetByEmailAsync(user?.Email);
            if (_user != null) return _user;*/

            //_user = user;  //new ApplicationUser { UserName = user.UserName, Email = user.Email, EmailConfirmed = true, FullName = user.FullName ?? user.Email, IsActive = true };
            await _userManager.CreateAsync(_user, Password);
            var isRoleExist = await _roleManager.RoleExistsAsync(UserTypes.User.ToString());
            if (!isRoleExist) await _roleManager.CreateAsync(new IdentityRole(UserTypes.User.ToString()));
            await _userManager.AddToRoleAsync(_user, UserTypes.User.ToString());

            // create profile
            var profileRepo = _unitOfWork.Repository<UserProfile>();
            await profileRepo.AddAsync(new UserProfile { UserId = _user.Id });
            if ((await _unitOfWork.CompleteAsync()) == 0) return false;

            return true;
        }

        public async Task<bool> UpdateUserAsync(ApplicationUser? user)
        {
            var _user = await GetByEmailAsync(user?.Email);
            if (_user is null) return false;

            var result = await _userManager.UpdateAsync(_user);
            return result.Succeeded;
        }

        public async Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user) => await _userManager.GeneratePasswordResetTokenAsync(user);

        public async Task<bool> ResetPasswordAsync(ApplicationUser user, string token, string newPassword)
        {
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            return result.Succeeded;
        }

        public async Task<bool> DeleteUserAsync(ApplicationUser user)
        {
            return false;
        }

        public async Task<IdentityResult> ChangePasswordAsync(ApplicationUser user, string currentPassword, string newPassword)
        => await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);


        public async Task AddAccessTokenToBlackListFromHeaderAsync()
        {// 1️⃣ قراءة الـ Authorization header
            var accessToken = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault()?.Replace("Bearer ", "");
            if (string.IsNullOrEmpty(accessToken))
                return; // مفيش توكن

            // 2️⃣ استخراج تاريخ انتهاء الصلاحية من الـ JWT
            var handler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwtToken;
            try
            {
                jwtToken = handler.ReadJwtToken(accessToken);
            }
            catch
            {
                // توكن غير صالح، نقدر نعمل لوج أو نرجع
                return;
            }

            var expiryDate = jwtToken.ValidTo;

            // 3️⃣ إضافة الـ Access Token للـ Blacklist
            var repo = _unitOfWork.Repository<BlacklistedToken>();
            var blacklistedToken = new BlacklistedToken
            {
                Token = accessToken,
                ExpiryDate = expiryDate
            };

            await repo.AddAsync(blacklistedToken);
            await _unitOfWork.CompleteAsync();
        }


    }
}
