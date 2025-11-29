using Base.DAL.Models.BaseModels;
using Base.DAL.Models.SystemModels;
using Base.Repo.Implementations;
using Base.Repo.Interfaces;
using Base.Services.Helpers;
using Base.Services.Interfaces;
using Base.Shared.DTOs;
using Base.Shared.Responses;
using Base.Shared.Responses.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using RepositoryProject.Specifications;
using System;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static Azure.Core.HttpHeader;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Base.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IUserService _userService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IJwtService _jwtService;
        private readonly IOtpService _otpService;
        private readonly ILogger<AuthService> _logger;
        private readonly IConfiguration _config;

        public AuthService(
            IUserService userService,
            IRefreshTokenService refreshTokenService,
            IJwtService jwtService,
            IOtpService otpService,
            ILogger<AuthService> logger,
            IConfiguration configuration)
        {
            _userService = userService;
            _refreshTokenService = refreshTokenService;
            _jwtService = jwtService;
            _otpService = otpService;
            _logger = logger;
            _config = configuration;
        }

        #region Login
        public async Task<LoginResult> LoginUserAsync(LoginDTO model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var user = await _userService.GetByEmailAsync(model.Email);
            if (user == null || !await _userService.CheckPasswordAsync(user, model.Password))
            {
                await Task.Delay(500); // Anti-brute-force delay
                return new LoginResult
                {
                    Success = false,
                    Message = "Invalid credentials.",
                    ErrorCode = "INVALID_CREDENTIALS"
                };
            }

            if (!user.EmailConfirmed)
            {
                await _otpService.ResendOtpAsync(user.Id, user.Email, "verifyemail");
                return new LoginResult
                {
                    Success = true,
                    Message = "Your account is not confirmed. OTP sent.",
                    Verification = new VerificationDetails
                    {
                        RequiresOtpVerification = user.TwoFactorEnabled,
                        EmailConfirmed = false,
                        Email = user.Email // Minimal info
                    }
                };
            }

            if (await _userService.IsLockedOutAsync(user))
            {
                return new LoginResult
                {
                    Success = false,
                    Message = "Account locked.",
                    ErrorCode = "ACCOUNT_LOCKED"
                };
            }

            if (user.TwoFactorEnabled)
            {
                await _otpService.ResendOtpAsync(user.Id, user.Email, "login");
                return new LoginResult
                {
                    Success = true,
                    Message = "OTP sent for login.",
                    Verification = new VerificationDetails
                    {
                        RequiresOtpVerification = true,
                        EmailConfirmed = true,
                        Email = user.Email
                    }
                };
            }

            // Full success
            var token = await _jwtService.GenerateJwtTokenAsync(user);
            var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(user.Id);
            var roles = await _userService.GetRolesAsync(user);
            var expiry = DateTime.UtcNow.AddMinutes(int.Parse(_config["Auth:Jwt:Minutes"] ?? "60")); // Example expiry; adjust based on JWT config

            return new LoginResult
            {
                Success = true,
                Message = "Login successful.",
                Auth = new AuthDetails { Token = token, RefreshToken = refreshToken, TokenExpiry = expiry },
                Verification = new VerificationDetails { RequiresOtpVerification = false, EmailConfirmed = true, Email = user.Email ?? "NA" },
                User = new UserDetails
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    UserType = user.Type,
                    Roles = roles
                }
            };
        }


        public async Task<LoginResult> VerifyLoginAsync(VerifyOtpDTO model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var (isValid, userId, errorCode) = await _otpService.ValidateOtpAsync(model.Email, model.Otp, "login");
            if (!isValid)
            {
                await Task.Delay(500); // Anti-brute-force delay
                return new LoginResult
                {
                    Success = false,
                    Message = "Invalid OTP.",
                    ErrorCode = "INVALID_OTP"
                };
            }

            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
            {
                return new LoginResult
                {
                    Success = false,
                    Message = "User not found.",
                    ErrorCode = "USER_NOT_FOUND"
                };
            }

            await _otpService.RemoveOtpAsync(model.Email, "login");

            // Optional: Re-check lockout or confirmation if needed, but assuming handled in initial login
            if (await _userService.IsLockedOutAsync(user))
            {
                return new LoginResult
                {
                    Success = false,
                    Message = "Account locked.",
                    ErrorCode = "ACCOUNT_LOCKED"
                };
            }

            var token = await _jwtService.GenerateJwtTokenAsync(user);
            var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(user.Id);
            var roles = await _userService.GetRolesAsync(user);
            var expiry = DateTime.UtcNow.AddMinutes(int.Parse(_config["Auth:Jwt:Minutes"] ?? "60")); // Example expiry; adjust based on JWT config

            return new LoginResult
            {
                Success = true,
                Message = "OTP verified successfully. Login complete.",
                Auth = new AuthDetails { Token = token, RefreshToken = refreshToken, TokenExpiry = expiry },
                Verification = new VerificationDetails { RequiresOtpVerification = false, EmailConfirmed = user.EmailConfirmed, Email = user.Email ?? "NA" }, // Defaults
                User = new UserDetails
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    UserType = user.Type,
                    Roles = roles
                }
            };
        }

        public async Task<ApiResponse> LogoutAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return new ApiResponse
                {
                    Success = false,
                    Message = "Invalid user ID.",
                    ErrorCode = "INVALID_USER_ID"
                };
            }

            // Assuming user existence check is not needed, but added for robustness
            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
            {
                return new ApiResponse
                {
                    Success = false,
                    Message = "User not found.",
                    ErrorCode = "USER_NOT_FOUND"
                };
            }

            await _refreshTokenService.RevokeAllUserTokensAsync(userId, "User logged out");
           
            // إضافة Access Token الحالي للـ Blacklist مباشرة من Header
            await _userService.AddAccessTokenToBlackListFromHeaderAsync();

            return new ApiResponse
            {
                Success = true,
                Message = "Logged out successfully."
            };
        }

        /*public async Task<ExternalLoginResponseDTO> HandleExternalLoginAsync(string email, string fullName)
        {
            var user = await _userService.GetByEmailAsync(email);
            if (user is null)

                user = await _userService.GetOrCreateExternalUserAsync(email, fullName);

            var token = await _jwtService.GenerateJwtTokenAsync(user);
            var roles = await _userService.GetRolesAsync(user);

            return new ExternalLoginResponseDTO
            {
                Token = token,
                user = new { user.Id, user.UserName, user.Email, Roles = roles }
            };
        }*/
        #endregion

        #region register
        public async Task<RegisterResult> RegisterAsync(RegisterDTO model, string ip, string userAgent)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));

            var existingUser = await _userService.GetByEmailAsync(model.Email);
            if (existingUser != null)
                return RegisterResult.EmailAlreadyExists();

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                TwoFactorEnabled = false,
                IsActive = true,
                Type = UserTypes.User,
                EmailConfirmed = false
            };

            var createResult = await _userService.CreateUserAsync(user, model.Password);
            if (!createResult)
                return new RegisterResult
                {
                    Success = false,
                    Message = "Registration failed. Please try again.",
                    ErrorCode = "REGISTRATION_FAILED"
                };

            // 4. إرسال OTP للتحقق من الإيميل
            var otpResult = await _otpService.GenerateAndSendOtpAsync(user.Id, user.Email, "verifyemail");

            if (!otpResult.Success)
            {
                _logger?.LogWarning("OTP failed after successful registration - UserId: {UserId}", user.Id);
                // ما نفشلش التسجيل كله بسبب الـ email، بس نسجل المشكلة
            }

            return RegisterResult.Successed(
                otpExpiresAt: otpResult.ExpiresAtUtc,
                remainingResends: otpResult.RemainingResends
            );
        }

        public async Task<VerifyEmailResult> VerifyEmailAsync(VerifyOtpDTO model)
        {
            var email = model.Email.ToLowerInvariant();

            // 1. تحقق من الـ OTP
            var (isValid, userId, errorCode) = await _otpService.ValidateOtpAsync(email, model.Otp, "verifyemail");

            if (!isValid)
            {
                return errorCode switch
                {
                    "OTP_EXPIRED" => VerifyEmailResult.InvalidOrExpired(),
                    "TOO_MANY_ATTEMPTS" => VerifyEmailResult.TooManyAttempts(),
                    _ => VerifyEmailResult.InvalidOrExpired()
                };
            }

            // 2. جيب المستخدم
            var user = await _userService.GetByIdAsync(userId!);
            if (user == null)
            {
                _logger?.LogWarning("OTP verified but user not found: {UserId}", userId);
                return VerifyEmailResult.InvalidOrExpired(); // نكدب للأمان
            }

            // 3. لو الإيميل متحقق منه أصلاً
            if (user.EmailConfirmed)
                return VerifyEmailResult.AlreadyVerified();

            // 4. تحديث حالة التحقق
            user.EmailConfirmed = true;

            var updateSuccess = await _userService.UpdateUserAsync(user);
            if (!updateSuccess)
            {
                _logger?.LogError("Failed to update EmailConfirmed for user {UserId}", user.Id);
                return new VerifyEmailResult { Success = false, Message = "Verification failed. Please try again." };
            }

            // 5. حذف الـ OTP المستخدم
            await _otpService.RemoveOtpAsync(email, "verifyemail");

            return VerifyEmailResult.Successed(DateTime.UtcNow);
        }
        #endregion

        #region forgetPassword
        public async Task<ForgotPasswordResult> ForgotPasswordAsync(string email)
        {
            // 1. Validation
            if (string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email))
                return ForgotPasswordResult.InvalidEmail();

            var user = await _userService.GetByEmailAsync(email);
            if (user == null)
                return ForgotPasswordResult.PrivacySafe();
            var otpResult = await _otpService.ResendOtpAsync(user.Id, user.Email, "reset");
            if (!otpResult.Success)
            {
                _logger?.LogWarning("OTP generation failed for password reset - User: {UserId}, Email: {Email}, Error: {ErrorCode}",
                    user.Id, user.Email, otpResult.ErrorCode);

                return ForgotPasswordResult.FromOtpResult(otpResult);
            }


            return ForgotPasswordResult.Successed(
                expiresAt: otpResult.ExpiresAtUtc,
                remainingResends: otpResult.RemainingResends
            );
        }

        public async Task<VerifyResetOtpResult> VerifyOtpAndGenerateResetTokenAsync(VerifyOtpDTO verifyOtpDTO)
        {
            verifyOtpDTO.Email = verifyOtpDTO.Email.ToLowerInvariant();

            // 1. تحقق من الـ OTP
            var (isValid, userId, errorCode) = await _otpService.ValidateOtpAsync(verifyOtpDTO.Email, verifyOtpDTO.Otp, "reset");
            if (!isValid)
            {
                await Task.Delay(500); // Anti-brute-force delay
                return errorCode switch
                {
                    "OTP_EXPIRED" => VerifyResetOtpResult.ExpiredOtp(),
                    "TOO_MANY_ATTEMPTS" => VerifyResetOtpResult.TooManyAttempts(),
                    _ => VerifyResetOtpResult.InvalidOtp()
                };
            }

            // 2. جيب المستخدم
            var user = await _userService.GetByIdAsync(userId!);
            if (user == null)
            {
                _logger?.LogWarning("OTP validated but user not found: {UserId}", userId);
                return VerifyResetOtpResult.InvalidOtp(); // نكدب عشان الأمان
            }

            // 3. توليد Reset Token (URL-safe, expires in 15 minutes)
            // Generate Identity reset token (URL-safe)
            var resetToken = await _userService.GeneratePasswordResetTokenAsync(user);
            var expiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_config["Auth:ResetTokenMinutes"] ?? "60"));

            return VerifyResetOtpResult.Successed(resetToken, expiresAt);
        }

        public async Task<ResetPasswordResult> ResetPasswordAsync(ResetPasswordDTO model)
        {
            var email = model.Email.ToLowerInvariant();

            // 2. جيب المستخدم
            var user = await _userService.GetByEmailAsync(email);
            if (user == null || !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                return ResetPasswordResult.InvalidOrExpired();
            }

            // 4. غيّر الباسوورد
            var changeResult = await _userService.ResetPasswordAsync(user, model.Token, model.NewPassword);

            if (!changeResult)
            {
                _logger?.LogWarning("Password reset failed for user {UserId}", user.Id);
                return new ResetPasswordResult
                {
                    Success = false,
                    Message = "Failed to change password.",
                    ErrorCode = "RESET_FAILED"
                };
            }

            // 7. Log مهم جدًا للأمان
            _logger?.LogInformation("Password reset successful for user {UserId}",
                user.Id);

            return ResetPasswordResult.Successed(DateTime.UtcNow);
        }
        #endregion

        #region 2FA
        public async Task<ApiResponse> InitiateEnable2FaAsync(string email)
        {
            var user = await _userService.GetByEmailAsync(email);
            if (user == null) return ApiResponse.Fail("User not found.");
            try
            {

                await _otpService.GenerateAndSendOtpAsync(user.Id, user.Email, "2fa");
                return ApiResponse.Ok(message: "Verification code sent successfully. Check your email.");
                //return new ApiResponse.Successed { Success = true, Message = "OTP sent to confirm enabling 2FA." };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send enable-2FA OTP to {Email}", email);
                return ApiResponse.Fail("Failed to send verification code. Please try again later.", "OTP_DELIVERY_FAILED");
                //return new BaseResponse { Success = false, Message = "Failed to send OTP." };
            }
        }

        public async Task<ApiResponse> ConfirmEnable2FaAsync(string email, string otp)
        {
            var (isValid, userId, errorCode) = await _otpService.ValidateOtpAsync(email, otp, "2fa");
            if (!isValid) return ApiResponse.Fail("Invalid or expired verification code.", "INVALID_OTP");

            var user = await _userService.GetByIdAsync(userId);
            if (user == null) return ApiResponse.NotFound();

            if (user.TwoFactorEnabled)
                return ApiResponse.Fail("2FA is already enabled.");

            user.TwoFactorEnabled = true;
            var ok = await _userService.UpdateUserAsync(user);
            if (!ok) return ApiResponse.Fail("Failed to enable 2FA. Please try again.");

            try { await _otpService.RemoveOtpAsync(email, "2fa"); } catch { }
            return ApiResponse.Ok("Two-Factor Authentication has been enabled successfully.");
        }

        public async Task<ApiResponse> Disable2FaAsync(string email, string currentPassword)
        {
            var user = await _userService.GetByEmailAsync(email);
            if (user == null) return ApiResponse.NotFound();
            if (!user.TwoFactorEnabled) return ApiResponse.Fail("2FA is not enabled.");

            // لازم الباسوورد + OTP معًا (أعلى أمان)
            var passwordValid = await _userService.CheckPasswordAsync(user, currentPassword);
            if (!passwordValid)
                return ApiResponse.Fail("Invalid password.", "INVALID_PASSWORD");

            user.TwoFactorEnabled = false;
            var ok = await _userService.UpdateUserAsync(user);
            if (!ok) return ApiResponse.Fail("Failed to disable Two-Factor.. Please try again.");

            return ApiResponse.Ok(message: "Two-Factor Authentication disabled successfully.");
        }
        #endregion

        #region refreshtoken
        public async Task<RefreshTokenResult> RefreshTokenAsync(RefreshTokenRequest request)
        {
            var token = request.RefreshToken.Trim();

            // 1. تحقق من الـ Refresh Token
            var validation = await _refreshTokenService.ValidateRefreshTokenAsync(token);

            if (!validation.IsValid)
            {
                return validation.Reason switch
                {
                    "EXPIRED" => RefreshTokenResult.InvalidOrExpired(),
                    "REVOKED" => RefreshTokenResult.Revoked(),
                    "NOT_FOUND" => RefreshTokenResult.InvalidOrExpired(),
                    _ => RefreshTokenResult.InvalidOrExpired()
                };
            }

            var userId = validation.UserId!;

            // 2. جيب المستخدم
            var user = await _userService.GetByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                // نكدب ونبطل التوكن عشان الأمان
                await _refreshTokenService.RevokeRefreshTokenAsync(token, "User not found or inactive");
                return RefreshTokenResult.InvalidOrExpired();
            }

            // 4. توليد JWT جديد + Refresh Token جديد (Rotation + Reuse Detection)
            var newJwt = await _jwtService.GenerateJwtTokenAsync(user);
            var newRefreshToken = await _refreshTokenService.CreateRefreshTokenAsync(user.Id);

            var roles = await _userService.GetRolesAsync(user);

            // Revoke old refresh token
            await _refreshTokenService.RevokeRefreshTokenAsync(token, "Token refreshed");
            // 6. Log مهم جدًا للأمان
            _logger?.LogInformation("Refresh token used successfully for user {UserId}",
                user.Id);

            return RefreshTokenResult.Successed(
                auth: new AuthDetails { Token = newJwt, RefreshToken = newRefreshToken },
                user: new UserDetails
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    UserType = user.Type,
                    Roles = roles
                }
            );
        }

        /*public async Task<LoginResult> RefreshTokenAsync(string refreshToken)
        {
            var userId = await _refreshTokenService.ValidateRefreshTokenAsync(refreshToken);
            if (userId == null)
                return new LoginResult { Success = false, Message = "Invalid or expired refresh token." };

            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                return new LoginResult { Success = false, Message = "User not found." };

            var token = await _jwtService.GenerateJwtTokenAsync(user);
            var newRefreshToken = await _refreshTokenService.CreateRefreshTokenAsync(user.Id);

            var roles = await _userService.GetRolesAsync(user);

            // Revoke old refresh token
            await _refreshTokenService.RevokeRefreshTokenAsync(refreshToken, "Token refreshed");

            return new LoginResult
            {
                Success = true,
                Auth = new AuthDetails() { Token = token, RefreshToken = newRefreshToken },
                User = new UserDetails() { Id = user.Id, UserName = user.UserName, Email = user.Email, UserType = user.UserType, Roles = roles }
            };
        }*/

        #endregion

        #region OTP
        public async Task<ResendOtpResult> ResendOtpAsync(string email, string purpose)
        {
            if (string.IsNullOrWhiteSpace(email) || !new EmailAddressAttribute().IsValid(email))
                return ResendOtpResult.InvalidEmail();

            var user = await _userService.GetByEmailAsync(email);
            if (user == null)
                return ResendOtpResult.PrivacySafe(); // "If the email is registered, OTP has been sent"

            var result = await _otpService.ResendOtpAsync(user.Id, email, purpose);

            if (!result.Success)
            {
                return new ResendOtpResult
                {
                    Success = false,
                    Message = result.Message,
                    ErrorCode = result.ErrorCode // مثل: RESEND_COOLDOWN, TOO_MANY_RESENDS
                };
            }

            return ResendOtpResult.Successed(result.ExpiresAtUtc, result.RemainingResends);
        }
        #endregion

        public async Task<ApiResponse> ChangePasswordAsync(string userId, ChangePasswordDTO model)
        {
            
            if (model is null) throw new ArgumentNullException(nameof(model));

            // 1. User Validation (Moved from controller, but checks the ID passed by controller)
            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
                return ApiResponse.Fail("User not found.", "USER_NOT_FOUND");

            var result = await _userService.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                var firstError = result.Errors.FirstOrDefault();
                var errorCode = firstError?.Code switch
                {
                    "PasswordMismatch" => "CURRENT_PASSWORD_INCORRECT",
                    "PasswordRequiresDigit" => "PASSWORD_REQUIRES_DIGIT",
                    "PasswordRequiresLower" => "PASSWORD_REQUIRES_LOWERCASE",
                    "PasswordRequiresUpper" => "PASSWORD_REQUIRES_UPPERCASE",
                    "PasswordRequiresNonAlphanumeric" => "PASSWORD_REQUIRES_SPECIAL_CHAR",
                    "PasswordTooShort" => "PASSWORD_TOO_SHORT",
                    _ => "INVALID_PASSWORD"
                };

                return ApiResponse.Fail(firstError?.Description ?? "Password change failed.", errorCode);
            }

            return ApiResponse.Ok("Password changed successfully.");
        }
    }
}