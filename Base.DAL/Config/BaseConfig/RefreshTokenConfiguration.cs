using Base.DAL.Models.BaseModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Base.DAL.Config.BaseConfig
{
    public class RefreshTokenConfiguration : BaseEntityConfigurations<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            base.Configure(builder);

            // TokenHash is unique
            builder.Property(r => r.TokenHash)
                .HasMaxLength(64)
                .IsRequired();

            builder.HasIndex(r => r.TokenHash)
                .IsUnique();

            // Required properties
            builder.Property(r => r.CreatedByIp)
                .HasMaxLength(45) // IPv4 + IPv6
                .IsRequired();

            builder.Property(r => r.CreatedByUserAgent)
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(r => r.RevokedByIp)
                .HasMaxLength(45);

            builder.Property(r => r.ReplacedByTokenHash)
                .HasMaxLength(64);

            builder.Property(r => r.ReasonRevoked)
                .HasMaxLength(256);

            // علاقة (User 1 ---- * RefreshTokens)
            builder.HasOne(r => r.User)
                   .WithMany()
                   .HasForeignKey(r => r.UserId)
                   .OnDelete(DeleteBehavior.SetNull);

            // RowVersion (Concurrency Token)
            builder.Property(r => r.RowVersion)
                .IsRowVersion();
        }
    }


}
