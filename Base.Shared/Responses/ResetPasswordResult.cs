using Base.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Shared.Responses
{
    // Response
    public class ResetPasswordResult : ApiResponse
    {
        public DateTime? PasswordChangedAt { get; set; }

        public static ResetPasswordResult InvalidOrExpired()
            => new() { Success = false, Message = "The reset link or code is invalid or has expired.", ErrorCode = "INVALID_RESET" };

        public static ResetPasswordResult TooManyAttempts()
            => new() { Success = false, Message = "Too many failed attempts. Please request a new reset link.", ErrorCode = "TOO_MANY_ATTEMPTS" };

        public static ResetPasswordResult WeakPassword()
            => new() { Success = false, Message = "The new password does not meet security requirements.", ErrorCode = "WEAK_PASSWORD" };

        public static ResetPasswordResult Successed(DateTime changedAt)
            => new()
            {
                Success = true,
                Message = "Password has been changed successfully. You can now log in with your new password.",
                PasswordChangedAt = changedAt
            };
    }
}
