using Base.DAL.Models.BaseModels;
using Base.Repo.Interfaces;
using Base.Services.Helpers;
using Base.Shared.DTOs;
using Base.Shared.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RepositoryProject.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

namespace Base.Services.Interfaces
{
    // Assumption: IOtpService interface definition (used by AuthController)
    public interface IOtpService
    {
        Task<(bool IsValid, string? UserId, string? ErrorCode)> ValidateOtpAsync(string email, string otp, string purpose);
        // 🛡️ إضافة وقائية: لحذف رمز OTP من المخزن بعد الاستخدام
        Task RemoveOtpAsync(string email, string purpose);
        Task<ResendOtpResult> GenerateAndSendOtpAsync(string userId, string email, string purpose);
        Task<OtpEntry?> GetActiveOtpAsync(string email, string purpose);
        Task<ResendOtpResult> ResendOtpAsync(string userId, string email, string purpose);
        Task AddAsync(OtpEntry otp);
        Task UpdateAsync(OtpEntry otp);
        Task DeleteAsync(OtpEntry otp);
    }
}
