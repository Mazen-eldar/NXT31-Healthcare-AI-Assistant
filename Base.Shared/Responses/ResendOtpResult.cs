using Base.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Shared.Responses
{
    public class ResendOtpResult : ApiResponse
    {
       // public string OtpCode { get; set; } // فقط للـ testing أو dev، في الـ production مش هنستخدمه
        public DateTime ExpiresAtUtc { get; set; }
        public int RemainingAttempts { get; set; }
        public int RemainingResends { get; set; }

        public static ResendOtpResult Successed(DateTime expiresAt, int remainingAttempts = 3, int remainingResends = 5)
            => new ResendOtpResult
            {
                Success = true,
                Message = "OTP sent successfully.",
                ExpiresAtUtc = expiresAt,
                RemainingAttempts = remainingAttempts,
                RemainingResends = remainingResends
            };

        public static ResendOtpResult RateLimit(int waitSeconds)
            => new ResendOtpResult
            {
                Success = false,
                Message = $"Please wait {waitSeconds} seconds before requesting a new OTP.",
                ErrorCode = "RESEND_COOLDOWN"
            };

        public static ResendOtpResult TooManyResends()
            => new ResendOtpResult
            {
                Success = false,
                Message = "Maximum resend attempts reached. Please try again after 1 hour.",
                ErrorCode = "TOO_MANY_RESENDS"
            };

        // 1. Invalid Email (400 Bad Request)
        public static ResendOtpResult InvalidEmail()
            => new ResendOtpResult
            {
                Success = false,
                Message = "Please provide a valid email address.",
                ErrorCode = "INVALID_EMAIL",
                //RequiresVerification = false
            };

        // 2. Privacy-Safe Response (202 Accepted) - أهم حاجة في الأمان
        public static ResendOtpResult PrivacySafe()
            => new ResendOtpResult
            {
                Success = true,
                Message = "If the email is registered, a new OTP has been sent.",
                //RequiresVerification = true,
                // ملاحظة مهمة: ما بنحطش ExpiresAt ولا RemainingResends هنا
                // عشان لو الإيميل مش موجود، ما نعطيش أي hint إضافي
            };
    }
}
