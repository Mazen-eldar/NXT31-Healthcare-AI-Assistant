using Base.DAL.Models.BaseModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Base.DAL.Config.BaseConfig
{
    public class OtpEntryConfiguration : BaseEntityConfigurations<OtpEntry>
    {
        public void Configure(EntityTypeBuilder<OtpEntry> builder)
        {
            base.Configure(builder);

            // علاقة (User 1 ---- * OTP)
            builder.HasOne(r => r.User)
                   .WithMany()
                   .HasForeignKey(r => r.UserId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }


}
