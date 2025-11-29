using Azure.Core;
using Base.DAL.Models.BaseModels;
using Base.Repo.Interfaces;
using Base.Services.Helpers;
using Base.Services.Interfaces;
using Base.Shared.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RepositoryProject.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Services.Implementations
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _config;

        public RefreshTokenService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContextAccessor, IConfiguration config)
        {
            _unitOfWork = unitOfWork;
            _httpContextAccessor = httpContextAccessor;
            _config = config;
        }

        public async Task<string> CreateRefreshTokenAsync(string userId)
        {
            var GeneratedToken = TokenGenerator.GenerateTokenWithHash();
            var plainToken = GeneratedToken.token;
            var hash = GeneratedToken.hash;

            var token = new RefreshToken
            {
                TokenHash = hash,
                UserId = userId,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                CreatedByUserAgent = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "unknown",
                ExpiresAtUtc = DateTime.UtcNow.AddDays(int.Parse(_config["Auth:RefreshTokenDays"] ?? "30"))
            };

            var repo = _unitOfWork.Repository<RefreshToken>();
            await repo.AddAsync(token);
            await _unitOfWork.CompleteAsync();

            return plainToken;
        }
        public async Task<RefreshTokenValidationResult> ValidateRefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return RefreshTokenValidationResult.Invalid("Refresh token is empty");

            var tokenHash = HashHelper.ComputeSha256Hash(refreshToken);

            var repo = _unitOfWork.Repository<RefreshToken>();

            var currentIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var currentUa = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "unknown";

            var hash = HashHelper.ComputeSha256Hash(refreshToken);
            var spec = new BaseSpecification<RefreshToken>(t => t.TokenHash == hash && t.RevokedAtUtc == null &&
            t.ExpiresAtUtc > DateTime.UtcNow &&
            t.CreatedByIp == currentIp &&
            t.CreatedByUserAgent == currentUa);
            var token = await repo.GetEntityWithSpecAsync(spec, true);

            // 1. التوكن مش موجود أو منتهي أو مرفوض
            if (token == null)
                return RefreshTokenValidationResult.Invalid("Invalid or expired refresh token");

            return RefreshTokenValidationResult.Valid(token.UserId, token.CreatedAtUtc);
        }

        /*public async Task<string?> ValidateRefreshTokenAsync(string refreshToken)
        {
            var currentIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var currentUa = _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString() ?? "unknown";

            var hash = HashHelper.ComputeSha256Hash(refreshToken);
            var repo = _unitOfWork.Repository<RefreshToken>();
            var spec = new BaseSpecification<RefreshToken>(t => t.TokenHash == hash && t.RevokedAtUtc == null &&
            t.ExpiresAtUtc > DateTime.UtcNow &&
            t.CreatedByIp == currentIp &&
            t.CreatedByUserAgent == currentUa);
            var token = await repo.GetEntityWithSpecAsync(spec, true);
            return token?.UserId;
        }*/

        public async Task RevokeRefreshTokenAsync(string refreshToken, string reason)
        {
            var hash = HashHelper.ComputeSha256Hash(refreshToken);
            var repo = _unitOfWork.Repository<RefreshToken>();
            var spec = new BaseSpecification<RefreshToken>(t => t.TokenHash == hash && t.RevokedAtUtc == null);
            var token = await repo.GetEntityWithSpecAsync(spec);
            if (token == null) return;
            token.RevokedAtUtc = DateTime.UtcNow;
            token.ReasonRevoked = reason;
            await _unitOfWork.CompleteAsync();
        }

        public async Task RevokeAllUserTokensAsync(string userId, string reason)
        {
            var repo = _unitOfWork.Repository<RefreshToken>();
            var tokens = await repo.ListAsync(new BaseSpecification<RefreshToken>(t => t.UserId == userId && t.RevokedAtUtc == null));
            foreach (var t in tokens)
            {
                t.RevokedAtUtc = DateTime.UtcNow;
                t.ReasonRevoked = reason;
            }
            await _unitOfWork.CompleteAsync();
        }
  



    }
}
