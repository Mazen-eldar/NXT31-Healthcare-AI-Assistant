using Base.DAL.Models.BaseModels;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.DAL.Models.SystemModels
{
    public class UserProfile:BaseEntity
    {
        //public int Id { get; set; }
        public string? UserId { get; set; } // FK إلى ApplicationUser

        // Navigation property
        public virtual ApplicationUser? User { get; set; }
    }
}
