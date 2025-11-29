using Base.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Shared.Responses
{
    public class RegisterResult : ApiResponse
    {
        public bool RequiresVerification { get; set; } = true;
        public DateTime? OtpExpiresAt { get; set; }
        public int RemainingResends { get; set; }

        public static RegisterResult EmailAlreadyExists()
            => new() { Success = false, Message = "This email is already registered.", ErrorCode = "EMAIL_EXISTS" };

        public static RegisterResult WeakPassword()
            => new() { Success = false, Message = "Password does not meet security requirements.", ErrorCode = "WEAK_PASSWORD" };

        public static RegisterResult TooManyRequests()
            => new() { Success = false, Message = "Too many registration attempts. Please try again later.", ErrorCode = "TOO_MANY_REQUESTS" };

        public static RegisterResult Successed(DateTime otpExpiresAt, int remainingResends = 5)
            => new()
            {
                Success = true,
                Message = "Account created successfully. Please check your email to verify your account.",
                RequiresVerification = true,
                OtpExpiresAt = otpExpiresAt,
                RemainingResends = remainingResends
            };
    }
}
