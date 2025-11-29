using Base.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Shared.Responses
{
    public class ForgotPasswordResult : ApiResponse
    {
        public bool RequiresVerification { get; set; } = true;
        public DateTime? OtpExpiresAt { get; set; }
        public int RemainingResends { get; set; }

        // إيميل شكله غلط
        public static ForgotPasswordResult InvalidEmail()
            => new() { Success = false, Message = "Please provide a valid email address.", ErrorCode = "INVALID_EMAIL" };

        // Privacy-Safe: ما نعرفش إذا الإيميل موجود ولا لأ
        public static ForgotPasswordResult PrivacySafe()
            => new()
            {
                Success = true,
                Message = "If this email is registered, a password reset code has been sent.",
                RequiresVerification = true
            };

        // نجاح حقيقي (المستخدم موجود والـ OTP اتبعت فعلاً)
        public static ForgotPasswordResult Successed(DateTime expiresAt, int remainingResends = 5)
            => new()
            {
                Success = true,
                Message = "Password reset code sent successfully.",
                RequiresVerification = true,
                OtpExpiresAt = expiresAt,
                RemainingResends = remainingResends
            };

        // تحويل من OtpResult لو فيه rate limit أو error
        public static ForgotPasswordResult FromOtpResult(ResendOtpResult otpResult)
            => new()
            {
                Success = otpResult.Success,
                Message = otpResult.Message,
                ErrorCode = otpResult.ErrorCode
            };
    }
}
