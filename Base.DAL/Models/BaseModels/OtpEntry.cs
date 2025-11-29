using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.DAL.Models.BaseModels
{
    public class OtpEntry : BaseEntity
    {
        public string UserId { get; set; } = null!;
        public virtual ApplicationUser? User { get; set; }
        public string Email { get; set; } = null!;  // مهم لو نفس المستخدم عنده أكثر من بريد أو تسجيل OTP لبريد مؤقت
        public string CodeHash { get; set; } = null!; // خزن الـ OTP بشكل مشفر/هاش
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime ExpiresAtUtc { get; set; }
        public bool IsUsed { get; set; } = false; // لحماية إعادة استخدام OTP
        public string? Purpose { get; set; } // مثال: "Login", "PasswordReset", "EmailVerification"
        public int Attempts { get; set; } = 0;
        public int ResendCount { get; set; } = 0;
        public DateTime? LastResendAt { get; set; }
    }

}
