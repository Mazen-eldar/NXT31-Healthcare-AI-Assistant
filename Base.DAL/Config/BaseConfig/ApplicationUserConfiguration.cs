using Base.DAL.Models.BaseModels;
using Base.DAL.Models.SystemModels;
using Base.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Base.DAL.Config.BaseConfig
{
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
    {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder)
        {
            builder.HasKey(u => u.Id);
            builder.Property(u => u.FullName).IsRequired().HasMaxLength(150);
            //builder.Property(u => u.UserType).IsRequired().HasMaxLength(50);

            builder.Property(u => u.Type).HasConversion(v => v.ToString(),
            v => (UserTypes)Enum.Parse(typeof(UserTypes), v));

            builder.HasOne(u => u.Profile)
                   .WithOne(p => p.User)
                   .HasForeignKey<UserProfile>(p => p.UserId).OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(u => u.SystemAdminProfile)
                   .WithOne(p => p.User)
                   .HasForeignKey<SystemAdminProfile>(p => p.UserId).OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(u => u.ClincAdminProfile)
                   .WithOne(p => p.User)
                   .HasForeignKey<ClincAdminProfile>(p => p.UserId).OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(u => u.ClincDoctorProfile)
                   .WithOne(p => p.User)
                   .HasForeignKey<ClincDoctorProfile>(p => p.UserId).OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(u => u.ClincReceptionistProfile)
                   .WithOne(p => p.User)
                   .HasForeignKey<ClincReceptionistProfile>(p => p.UserId).OnDelete(DeleteBehavior.Cascade);

        }
    }
}
