using Base.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Shared.Responses
{
    public class RefreshTokenResult : ApiResponse
    {
        public AuthDetails? Auth { get; set; }
        public UserDetails? User { get; set; }

        // نجاح
        public static RefreshTokenResult Successed(AuthDetails auth, UserDetails user)
            => new()
            {
                Success = true,
                Message = "Token refreshed successfully.",
                Auth = auth,
                User = user
            };

        // فشل عام
        public static RefreshTokenResult InvalidOrExpired()
            => new() { Success = false, Message = "The refresh token is invalid or has expired.", ErrorCode = "INVALID_REFRESH_TOKEN" };

        // تم إبطاله (revoked)
        public static RefreshTokenResult Revoked()
            => new() { Success = false, Message = "This refresh token has been revoked.", ErrorCode = "TOKEN_REVOKED" };

        // كثير محاولات (optional لو عندك rate limiting)
        public static RefreshTokenResult TooManyAttempts()
            => new() { Success = false, Message = "Too many refresh attempts. Please log in again.", ErrorCode = "TOO_MANY_ATTEMPTS" };
    }
}
