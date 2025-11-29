using Base.DAL.Config.BaseConfig;
using Base.DAL.Models.SystemModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Base.DAL.Config.SystemConfig
{
    // ClinicSchedule Configuration
    public class ClinicScheduleConfiguration : BaseEntityConfigurations<ClinicSchedule>
    {
        public override void Configure(EntityTypeBuilder<ClinicSchedule> builder)
        {
            base.Configure(builder);

            builder.HasOne(s => s.Clinic)
                   .WithMany(c => c.ClinicSchedules)
                   .HasForeignKey(s => s.ClinicId);

            builder.HasOne(s => s.Doctor)
                   .WithMany(d => d.Schedules)
                   .HasForeignKey(s => s.DoctorId)
                   .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(s => s.AppointmentSlots)
                   .WithOne(a => a.DoctorSchedule)
                   .HasForeignKey(a => a.ClinicScheduleId);
        }
    }


}
