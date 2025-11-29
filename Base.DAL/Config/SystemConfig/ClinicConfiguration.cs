using Base.DAL.Config.BaseConfig;
using Base.DAL.Models.SystemModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Base.DAL.Config.SystemConfig
{
    // Clinic Configuration
    public class ClinicConfiguration : BaseEntityConfigurations<Clinic>
    {
        public override void Configure(EntityTypeBuilder<Clinic> builder)
        {
            base.Configure(builder);

            builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
            builder.Property(c => c.Email).HasMaxLength(100);
            builder.Property(c => c.Status).HasMaxLength(50);

            builder.HasMany(c => c.ClincAdmin)
                   .WithOne(a => a.Clinc)
                   .HasForeignKey(a => a.ClincId)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(c => c.ClincDoctor)
                   .WithOne(d => d.Clinc)
                   .HasForeignKey(d => d.ClincId)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(c => c.ClincReceptionis)
                   .WithOne(r => r.Clinc)
                   .HasForeignKey(r => r.ClincId)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(c => c.ClinicSchedules)
                   .WithOne(s => s.Clinic)
                   .HasForeignKey(s => s.ClinicId);
        }
    }


}
