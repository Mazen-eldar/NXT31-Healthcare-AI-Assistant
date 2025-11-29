using Base.DAL.Models.BaseModels;

namespace Base.DAL.Models.SystemModels
{
    public class ClinicSchedule : BaseEntity
    {
        public string ClinicId { get; set; }
        public virtual Clinic Clinic { get; set; }
        public string? DoctorId { get; set; }
        public virtual ClincDoctorProfile Doctor { get; set; }
        public DayOfWeek Day { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int SlotDurationMinutes { get; set; }
        public virtual ICollection<AppointmentSlot> AppointmentSlots { get; set; } = new List<AppointmentSlot>();
    }
}
