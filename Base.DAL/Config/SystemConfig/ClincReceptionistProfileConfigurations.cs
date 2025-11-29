using Base.DAL.Config.BaseConfig;
using Base.DAL.Models.SystemModels;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Base.DAL.Config.SystemConfig
{
    public class ClincReceptionistProfileConfigurations : BaseEntityConfigurations<ClincReceptionistProfile>
    {
        public override void Configure(EntityTypeBuilder<ClincReceptionistProfile> builder)
        {
            base.Configure(builder);
            /*builder.HasOne(p => p.User)
                   .WithOne() // نستخدم WithMany() لتجنب غموض One-to-One
                   .HasForeignKey<UserProfile>(p => p.UserId);
                   //.OnDelete(DeleteBehavior.SetNull);*/
        }
    }
}
