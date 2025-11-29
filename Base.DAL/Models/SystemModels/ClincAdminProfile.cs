using Base.DAL.Models.BaseModels;
using System.ComponentModel.DataAnnotations.Schema;

namespace Base.DAL.Models.SystemModels
{
    public class ClincAdminProfile : BaseEntity
    {
        public string? UserId { get; set; }
        public string? ClincId { get; set; }
        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser? User { get; set; }

        [ForeignKey(nameof(ClincId))]
        public virtual Clinic? Clinc { get; set; }
    }
}
