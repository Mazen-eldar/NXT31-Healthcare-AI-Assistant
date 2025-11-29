using Base.DAL.Models.BaseModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Base.Services.Interfaces
{
    public interface IJwtService
    {
        Task<string> GenerateJwtTokenAsync(ApplicationUser user);
        Task<ClaimsPrincipal> ValidateTokenAsync(string token);
        Task<string> GetUserIdFromTokenAsync(string token);
    }
}
