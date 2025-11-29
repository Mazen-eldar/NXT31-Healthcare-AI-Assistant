using Base.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Shared.Responses
{
    // Response
    public class VerifyEmailResult : ApiResponse
    {
        public bool EmailConfirmed { get; set; }
        public DateTime? VerifiedAt { get; set; }

        public static VerifyEmailResult InvalidOrExpired()
            => new() { Success = false, Message = "The verification code is invalid or has expired.", ErrorCode = "INVALID_OTP" };

        public static VerifyEmailResult AlreadyVerified()
            => new() { Success = false, Message = "This email is already verified.", ErrorCode = "ALREADY_VERIFIED" };

        public static VerifyEmailResult TooManyAttempts()
            => new() { Success = false, Message = "Too many incorrect attempts. Please request a new code.", ErrorCode = "TOO_MANY_ATTEMPTS" };

        public static VerifyEmailResult Successed(DateTime verifiedAt)
            => new()
            {
                Success = true,
                Message = "Email verified successfully! You can now log in.",
                EmailConfirmed = true,
                VerifiedAt = verifiedAt
            };
    }
}
