using Base.DAL.Config.BaseConfig;
using Base.DAL.Models.SystemModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Base.DAL.Config.SystemConfig
{
    // Appointment Configuration
    public class AppointmentConfiguration : BaseEntityConfigurations<Appointment>
    {
        public override void Configure(EntityTypeBuilder<Appointment> builder)
        {
            base.Configure(builder);

            builder.HasOne(a => a.Patient)
                   .WithMany()
                   .HasForeignKey(a => a.PatientId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }


}
