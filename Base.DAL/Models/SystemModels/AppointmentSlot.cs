using Base.DAL.Models.BaseModels;

namespace Base.DAL.Models.SystemModels
{
    public class AppointmentSlot : BaseEntity
    {
        public string ClinicScheduleId { get; set; }
        public virtual ClinicSchedule DoctorSchedule { get; set; }

        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsBooked { get; set; }

        public virtual Appointment? Appointment { get; set; }
    }
}
