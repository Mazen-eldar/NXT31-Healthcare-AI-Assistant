using Base.DAL.Models.BaseModels;
using Base.Repo.Interfaces;
using Base.Services.Helpers;
using Base.Services.Implementations;
using Base.Shared.DTOs;
using Base.Shared.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using RepositoryProject.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Services.Interfaces
{
    public interface IRefreshTokenService
    {
        Task<string> CreateRefreshTokenAsync(string userId);
        Task<RefreshTokenValidationResult> ValidateRefreshTokenAsync(string refreshToken);
        //Task<string?> ValidateRefreshTokenAsync(string refreshToken);
        Task RevokeRefreshTokenAsync(string refreshToken, string reason);
        Task RevokeAllUserTokensAsync(string userId, string reason);
    }
}
