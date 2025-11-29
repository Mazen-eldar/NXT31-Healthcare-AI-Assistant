using Base.DAL.Models.BaseModels;
using Base.DAL.Models.SystemModels;
using Base.Repo.Interfaces;
using Base.Services.Helpers;
using Base.Services.Interfaces;
using Base.Shared.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RepositoryProject.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Services.Implementations
{
    public class UserProfileService : IUserProfileService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserProfileService(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<UserDto?> GetByIdAsync(string id)
        {
            var user = await _userManager.Users
                //.Include(u => u.Farms)
                .FirstOrDefaultAsync(u => u.Id == id);

            return user?.ToUserDto();
        }
        public async Task<UserDto> CreateAsync(CreateUserRequest request)
        {
            var user = request.ToApplicationUser();
            user.IsActive = true;
            var result = await _userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded)
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

            return user.ToUserDto();
        }
        public async Task<UserDto?> UpdateAsync(string id, UpdateUserRequest request)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return null;

            if (!string.IsNullOrEmpty(request.FullName))
                user.FullName = request.FullName;

            if (request.UserType.HasValue)
                user.Type = request.UserType ?? user.Type;

            if (request.IsActive.HasValue)
                user.IsActive = request.IsActive.Value;

            if (!string.IsNullOrEmpty(request.ImagePath))
                user.ImagePath = request.ImagePath;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new Exception(string.Join(", ", result.Errors.Select(e => e.Description)));

            return user.ToUserDto();
        }
        public async Task<bool> ToggleActiveAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return false;

            user.IsActive = !user.IsActive;

            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }
        public async Task<bool> DeleteAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return false;

            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded;
        }
        public async Task<bool> ChangePasswordAsync(string userId, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) return false;

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
            return result.Succeeded;
        }
        public async Task<UserListDto> GetAllAsync(string? search, UserTypes? userType, bool? isActive, int page, int pageSize)
        {
            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));

            if (userType.HasValue)
                query = query.Where(u => u.Type == userType.Value);

            if (isActive.HasValue)
                query = query.Where(u => u.IsActive == isActive.Value);

            var total = await query.CountAsync();

            //query.Include(u => u.Farms);

            var users = await query
                .OrderBy(u => u.FullName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userDtos = users.ToUserDtoSet();// _mapper.Map<List<UserDto>>(users);

            return new UserListDto
            {
                Users = userDtos.ToList(),
                TotalCount = total,
                FilteredCount = userDtos.Count
            };
        }

    }
}

