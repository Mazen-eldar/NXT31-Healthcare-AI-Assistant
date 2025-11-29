using Base.DAL.Models.BaseModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Base.DAL.Models.SystemModels
{
    public class SystemAdminProfile : BaseEntity
    {
        //public int Id { get; set; }
        public string? UserId { get; set; }

        // Navigation property
        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser? User { get; set; }
    }
}
