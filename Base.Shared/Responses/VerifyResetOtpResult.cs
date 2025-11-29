using Base.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Shared.Responses
{
    public class VerifyResetOtpResult : ApiResponse
    {
        public string? ResetToken { get; set; }
        public DateTime? ExpiresAt { get; set; }

        public static VerifyResetOtpResult InvalidOtp()
            => new() { Success = false, Message = "The code you entered is incorrect.", ErrorCode = "INVALID_OTP" };

        public static VerifyResetOtpResult ExpiredOtp()
            => new() { Success = false, Message = "This code has expired. Please request a new one.", ErrorCode = "OTP_EXPIRED" };

        public static VerifyResetOtpResult TooManyAttempts()
            => new() { Success = false, Message = "Too many incorrect attempts. Please request a new code.", ErrorCode = "TOO_MANY_ATTEMPTS" };

        public static VerifyResetOtpResult Successed(string resetToken, DateTime expiresAt)
            => new()
            {
                Success = true,
                Message = "OTP verified successfully.",
                ResetToken = resetToken,
                ExpiresAt = expiresAt
            };
    }
}
