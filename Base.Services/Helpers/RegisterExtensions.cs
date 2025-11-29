using Base.DAL.Models.BaseModels;
using Base.DAL.Models.SystemModels;
using Base.Shared.DTOs;
using Microsoft.AspNetCore.Identity;

namespace Base.Services.Helpers

{
    public static class RegisterExtensions
    {
        public static ApplicationUser ToUser(this RegisterDTO Dto)
        {
            if (Dto is null)
            {
                return new ApplicationUser();
            }

            return new ApplicationUser
            {
                FullName = Dto.FullName,
                UserName = Dto.Email,
                Email = Dto.Email,
                PhoneNumber = Dto.PhoneNumber
            };
        }

        public static UserProfile ToProfile(this RegisterDTO Dto)
        {
            if (Dto is null)
            {
                return new UserProfile();
            }

            return new UserProfile
            {
                //FullName = Dto.FullName,
                //PhoneNumber = Dto.PhoneNumber
            };
        }
    }
}
