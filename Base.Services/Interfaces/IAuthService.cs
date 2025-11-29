using Base.DAL.Models;
using Base.Shared.DTOs;
using Base.Shared.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Base.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResult> LoginUserAsync(LoginDTO model);
        Task<LoginResult> VerifyLoginAsync(VerifyOtpDTO model);
        Task<ApiResponse> LogoutAsync(string userId);
        Task<RegisterResult> RegisterAsync(RegisterDTO model, string ip, string userAgent);
        Task<VerifyEmailResult> VerifyEmailAsync(VerifyOtpDTO model);
        Task<ResetPasswordResult> ResetPasswordAsync(ResetPasswordDTO model);
        //Task<ResetPasswordResult> ResetPasswordAsync(string email, string token, string newPassword);
        Task<ForgotPasswordResult> ForgotPasswordAsync(string email);
        Task<VerifyResetOtpResult> VerifyOtpAndGenerateResetTokenAsync(VerifyOtpDTO verifyOtpDTO);
        Task<ApiResponse> InitiateEnable2FaAsync(string email);
        Task<ApiResponse> ConfirmEnable2FaAsync(string email, string otp);
        Task<ApiResponse> Disable2FaAsync(string email, string currentPassword);
        //Task<LoginResult> RefreshTokenAsync(string refreshToken);
        Task<RefreshTokenResult> RefreshTokenAsync(RefreshTokenRequest request);
        Task<ResendOtpResult> ResendOtpAsync(string email, string purpose);
        Task<ApiResponse> ChangePasswordAsync(string userId, ChangePasswordDTO model);
    }
}
