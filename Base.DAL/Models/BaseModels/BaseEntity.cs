using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Base.DAL.Models.BaseModels
{
    public class BaseEntity
    {
        public string Id { get; set; }
        public string? CreatedById { get; set; }
        //public virtual ApplicationUser? CreatedBy { get; set; }
        public DateTime DateOfCreattion { get; set; }

        public string? UpdatedById { get; set; }
        //public virtual ApplicationUser? UpdatedBy { get; set; }
        public DateTime DateOfUpdate { get; set; }
        public BaseEntity()
        {
            Id = Guid.NewGuid().ToString();
        }
    }
}
