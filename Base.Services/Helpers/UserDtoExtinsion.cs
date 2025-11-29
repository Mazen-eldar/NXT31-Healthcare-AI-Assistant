using Base.DAL.Models.BaseModels;
using Base.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Services.Helpers
{
    public static class UserDtoExtinsion
    {
        public static UserDto ToUserDto(this ApplicationUser user)
        {
            if (user is null)
            {
                return new UserDto();
            }

            return new UserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                UserName = user.UserName ?? "NA",
                Email = user.Email ?? "NA",
                PhoneNumber = user.PhoneNumber,
                //UserType = user.UserType,
                UserType= user.Type,
                IsActive = user.IsActive,
                ImagePath = user.ImagePath
            };
        }

        public static HashSet<UserDto> ToUserDtoSet(this IEnumerable<ApplicationUser> entities)
        {
            if (entities == null)
                return new HashSet<UserDto>();

            return entities.Select(e => e.ToUserDto()).ToHashSet();
        }

        public static ApplicationUser ToApplicationUser(this CreateUserRequest Dto)
        {
            if (Dto is null)
            {
                return new ApplicationUser();
            }

            return new ApplicationUser
            {
                FullName = Dto.FullName,
                UserName = Dto.Email ?? "NA",
                Email = Dto.Email ?? "NA",
                PhoneNumber = Dto.PhoneNumber,
                Type = Dto.UserType,
            };

        }
    }
}
