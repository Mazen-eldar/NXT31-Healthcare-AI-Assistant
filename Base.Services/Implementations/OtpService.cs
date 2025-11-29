using Base.DAL.Models;
using Base.DAL.Models.BaseModels;
using Base.Repo.Interfaces;
using Base.Services.Helpers;
using Base.Services.Interfaces;
using Base.Shared.DTOs;
using Base.Shared.Responses;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RepositoryProject.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Services.Implementations
{
    // تنفيذ واجهة IOtpService باستخدام IMemoryCache
    public class OtpService : IOtpService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly ILogger<OtpService> _logger;
        private readonly IConfiguration _configuration;

        public OtpService(ILogger<OtpService> logger, IConfiguration configuration, IEmailService emailService, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _configuration = configuration;
            _emailService = emailService;
            _unitOfWork = unitOfWork;
        }

        public async Task<ResendOtpResult> GenerateAndSendOtpAsync(string userId, string email, string purpose)
        {
            //var otpCode = new Random().Next(100000, 999999).ToString();
            var otp = OtpGenerator.GenerateOtpWithHash();
            var entry = new OtpEntry
            {
                UserId = userId,
                Email = email,
                CodeHash = otp.hash,
                Purpose = purpose,
                CreatedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Auth:OtpSettings:ExpirationMinutes"] ?? "5")),
                Attempts = 0,
                ResendCount = 0
            };
            await AddAsync(entry);
            await _emailService.SendOtpEmailAsync(email, otp.otp);
            return ResendOtpResult.Successed(
                expiresAt: entry.ExpiresAtUtc,
                remainingAttempts: 3,
                remainingResends: 5
            );
        }

        public async Task<ResendOtpResult> ResendOtpAsync(string userId, string email, string purpose)
        {
            // 1) هات الـ OTP الحالي لو موجود ولسه ساري
            var otp = await GetActiveOtpAsync(email, purpose);

            // 2) لو مفيش OTP → ابعت واحد جديد
            if (otp == null)
                return await GenerateAndSendOtpAsync(userId, email, purpose);


            // 1) Prevent spam: wait 60 seconds
            if (otp.LastResendAt.HasValue &&
                otp.LastResendAt.Value.AddSeconds(60) > DateTime.UtcNow)
            {
                var next = otp.LastResendAt.Value.AddSeconds(60) - DateTime.UtcNow;
                return ResendOtpResult.RateLimit(next.Seconds);
            }

            // 2) Limit to 5 resend per hour
            if (otp.ResendCount >= 5 &&
                otp.LastResendAt.HasValue &&
                otp.LastResendAt.Value.AddHours(1) > DateTime.UtcNow)
            {
                return ResendOtpResult.TooManyResends();
            }


            // 3) Generate new OTP
            var (newOtp, newHash) = OtpGenerator.GenerateOtpWithHash();
            otp.CodeHash = newHash;
            otp.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Auth:OtpSettings:ExpirationMinutes"] ?? "5"));

            otp.ResendCount += 1;
            otp.LastResendAt = DateTime.UtcNow;
            otp.Attempts = 0;

            await UpdateAsync(otp);

            await _emailService.SendOtpEmailAsync(email, newOtp);
            int remainingResends = 5 - otp.ResendCount;
            return ResendOtpResult.Successed(
                expiresAt: otp.ExpiresAtUtc,
                remainingAttempts: 3,
                remainingResends: remainingResends >= 0 ? remainingResends : 0
            );
        }

        public async Task<(bool IsValid, string? UserId, string? ErrorCode)> ValidateOtpAsync(string email, string otp, string purpose)
        {
            var entry = await GetActiveOtpAsync(email, purpose);

            if (entry == null)
                return (false, null, "OTP_EXPIRED");

            if (entry.Attempts >= 5)
                return (false, null, "TOO_MANY_ATTEMPTS");

            var otphased = HashHelper.ComputeSha256Hash(otp);
            if (otphased != entry.CodeHash)
            {
                entry.Attempts += 1;
                await UpdateAsync(entry);

                if (entry.Attempts >= 5)
                    return (false, null, "TOO_MANY_ATTEMPTS");

                return (false, null, "INVALID_OTP");
            }

            // OTP صحيح
            entry.IsUsed = true;
            await UpdateAsync(entry);

            return (true, entry.UserId, null);
        }

        public async Task RemoveOtpAsync(string email, string purpose)
        {
            var Otp = await GetActiveOtpAsync(email, purpose);
            if (Otp is not null)
                await DeleteAsync(Otp);
        }

        public async Task<OtpEntry?> GetActiveOtpAsync(string email, string purpose)
        {
            var repo = _unitOfWork.Repository<OtpEntry>();
            var spec = new BaseSpecification<OtpEntry>(o => o.Email == email && o.Purpose == purpose && o.ExpiresAtUtc > DateTime.UtcNow);
            spec.AddOrderByDesc(e => e.CreatedAtUtc);
            var entry = await repo.GetEntityWithSpecAsync(spec);
            return entry;
        }

        public async Task AddAsync(OtpEntry otp)
        {
            var repo = _unitOfWork.Repository<OtpEntry>();
            await repo.AddAsync(otp);
            await _unitOfWork.CompleteAsync();
        }

        public async Task UpdateAsync(OtpEntry otp)
        {
            var repo = _unitOfWork.Repository<OtpEntry>();
            await repo.UpdateAsync(otp);
            await _unitOfWork.CompleteAsync();
        }

        public async Task DeleteAsync(OtpEntry otp)
        {
            var repo = _unitOfWork.Repository<OtpEntry>();
            await repo.DeleteAsync(otp);
            await _unitOfWork.CompleteAsync();
        }
    }
}
