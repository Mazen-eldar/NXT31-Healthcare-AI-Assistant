using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Shared.Responses
{
    public class RefreshTokenValidationResult
    {
        public bool IsValid { get; private set; }
        public string? UserId { get; private set; }
        public DateTime IssuedAt { get; private set; }
        public string Reason { get; private set; } = string.Empty;

        private RefreshTokenValidationResult() { }

        public static RefreshTokenValidationResult Valid(string userId, DateTime issuedAt)
            => new() { IsValid = true, UserId = userId, IssuedAt = issuedAt };

        public static RefreshTokenValidationResult Invalid(string reason)
            => new() { IsValid = false, Reason = reason };

        public static RefreshTokenValidationResult RevokedDueToReuse()
            => new() { IsValid = false, Reason = "Refresh token has been used before. All sessions revoked." };
    }
}
