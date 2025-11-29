using Base.Shared.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Shared.Responses
{
    public class LoginResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } // User-friendly message
        public string ErrorCode { get; set; } // For client-side programmatic handling (e.g., "INVALID_CREDENTIALS")
        public AuthDetails Auth { get; set; } // Nullable; tokens on full success
        public VerificationDetails Verification { get; set; } // Nullable; for OTP/email needs
        public UserDetails User { get; set; } // Nullable; only on full success
    }

    public class AuthDetails
    {
        public string Token { get; set; }
        public string RefreshToken { get; set; }
        public DateTime TokenExpiry { get; set; } // Added for better client management
    }

    public class VerificationDetails
    {
        public bool RequiresOtpVerification { get; set; }
        public bool EmailConfirmed { get; set; }
        public string Email { get; set; } // Minimal info for resend prompts
    }

    public class UserDetails
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public UserTypes UserType { get; set; }
        public IEnumerable<string> Roles { get; set; }
    }
}
