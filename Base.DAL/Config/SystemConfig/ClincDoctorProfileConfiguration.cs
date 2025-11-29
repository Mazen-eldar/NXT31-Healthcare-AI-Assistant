using Base.DAL.Config.BaseConfig;
using Base.DAL.Models.SystemModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Base.DAL.Config.SystemConfig
{
    // ClincDoctorProfile Configuration
    public class ClincDoctorProfileConfiguration : BaseEntityConfigurations<ClincDoctorProfile>
    {
        public override void Configure(EntityTypeBuilder<ClincDoctorProfile> builder)
        {
            base.Configure(builder);

            builder.HasMany(d => d.Schedules)
                   .WithOne(s => s.Doctor)
                   .HasForeignKey(s => s.DoctorId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }


}
