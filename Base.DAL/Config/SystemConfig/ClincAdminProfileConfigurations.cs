using Base.DAL.Config.BaseConfig;
using Base.DAL.Models.SystemModels;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Base.DAL.Config.SystemConfig
{
    public class ClincAdminProfileConfigurations : BaseEntityConfigurations<ClincAdminProfile>
    {
        public override void Configure(EntityTypeBuilder<ClincAdminProfile> builder)
        {
            base.Configure(builder);
            /*builder.HasOne(p => p.User)
                   .WithOne() // نستخدم WithMany() لتجنب غموض One-to-One
                   .HasForeignKey<UserProfile>(p => p.UserId);
                   //.OnDelete(DeleteBehavior.SetNull);*/
        }
    }
}
