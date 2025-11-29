using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Base.Services.Helpers
{
    public static class OtpGenerator
    {
        public static string GenerateOTP(int length = 6)
        {
            const string digits = "0123456789";
            char[] otp = new char[length];

            using var rng = RandomNumberGenerator.Create();
            byte[] buffer = new byte[1];

            for (int i = 0; i < length; i++)
            {
                rng.GetBytes(buffer);
                otp[i] = digits[buffer[0] % digits.Length];
            }
            return new string(otp);
        }

        public static (string otp, string hash) GenerateOtpWithHash(int length = 6)
        {
            string otp = GenerateOTP(length);
            string hash = HashHelper.ComputeSha256Hash(otp);
            return (otp, hash);
        }
    }
}
