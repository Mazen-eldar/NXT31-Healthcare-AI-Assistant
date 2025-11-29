using Base.DAL.Config.BaseConfig;
using Base.DAL.Models.SystemModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.DAL.Config.SystemConfig
{
    public class UserProfileConfigurations : BaseEntityConfigurations<UserProfile>
    {
        public override void Configure(EntityTypeBuilder<UserProfile> builder)
        {
            base.Configure(builder);
            /*builder.HasOne(p => p.User)
                   .WithOne() // نستخدم WithMany() لتجنب غموض One-to-One
                   .HasForeignKey<UserProfile>(p => p.UserId);
                   //.OnDelete(DeleteBehavior.SetNull);*/
        }
    }
}
