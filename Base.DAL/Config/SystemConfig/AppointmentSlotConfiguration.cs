using Base.DAL.Config.BaseConfig;
using Base.DAL.Models.SystemModels;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Base.DAL.Config.SystemConfig
{
    // AppointmentSlot Configuration
    public class AppointmentSlotConfiguration : BaseEntityConfigurations<AppointmentSlot>
    {
        public override void Configure(EntityTypeBuilder<AppointmentSlot> builder)
        {
            base.Configure(builder);

            builder.HasOne(a => a.DoctorSchedule)
                   .WithMany(s => s.AppointmentSlots)
                   .HasForeignKey(a => a.ClinicScheduleId);

            builder.HasOne(a => a.Appointment)
                   .WithOne(ap => ap.Slot)
                   .HasForeignKey<Appointment>(ap => ap.SlotId);
        }
    }


}
