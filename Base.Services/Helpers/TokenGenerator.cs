using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Base.Services.Helpers
{
    public static class TokenGenerator
    {
        public static string GenerateToken(int byteSize = 64)
        {
            var bytes = new byte[byteSize];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        public static (string token, string hash) GenerateTokenWithHash(int byteSize = 64)
        {
            string token = GenerateToken(byteSize);
            string hash = HashHelper.ComputeSha256Hash(token);
            return (token, hash);
        }
    }
}
