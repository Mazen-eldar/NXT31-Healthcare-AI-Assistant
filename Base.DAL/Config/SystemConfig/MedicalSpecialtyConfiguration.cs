using Base.DAL.Config.BaseConfig;
using Base.DAL.Models.SystemModels;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Base.DAL.Config.SystemConfig
{
    // MedicalSpecialty Configuration
    public class MedicalSpecialtyConfiguration : BaseEntityConfigurations<MedicalSpecialty>
    {
        public override void Configure(EntityTypeBuilder<MedicalSpecialty> builder)
        {
            base.Configure(builder);
            builder.Property(m => m.Name).IsRequired().HasMaxLength(100);
        }
    }


}
