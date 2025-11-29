using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.DAL.Models.BaseModels
{
    public class RefreshToken : BaseEntity
    {
        public string TokenHash { get; set; } = null!; // SHA256 hash of token value
        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public string? CreatedByIp { get; set; } = null!;
        public string CreatedByUserAgent { get; set; } = null!;
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime? RevokedAtUtc { get; set; }
        public string? RevokedByIp { get; set; }
        public string? ReplacedByTokenHash { get; set; }
        public string? ReasonRevoked { get; set; }
        public bool IsActive => RevokedAtUtc == null && DateTime.UtcNow < ExpiresAtUtc;

        // Concurrency token (row version) to help with concurrent updates
        [Timestamp]
        public byte[] RowVersion { get; set; } = null!;
    }
}
