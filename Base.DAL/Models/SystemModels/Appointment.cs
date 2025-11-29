using Base.DAL.Models.BaseModels;

namespace Base.DAL.Models.SystemModels
{
    public class Appointment : BaseEntity
    {
        public string SlotId { get; set; }
        public virtual AppointmentSlot Slot { get; set; }
        public string? PatientId { get; set; }
        public virtual UserProfile? Patient { get; set; }
        public string Reason { get; set; }
    }
}
