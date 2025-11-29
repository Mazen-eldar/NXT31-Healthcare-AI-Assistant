using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.Shared.DTOs
{
    public record RefreshTokenRequest(string RefreshToken, string? UserAgent = null, string? Ip = null);
    public record RefreshTokenResponse(string AccessToken, string RefreshToken);
}
