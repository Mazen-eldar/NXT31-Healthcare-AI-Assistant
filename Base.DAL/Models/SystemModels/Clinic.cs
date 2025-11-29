using Base.DAL.Models.BaseModels;

namespace Base.DAL.Models.SystemModels
{
    public class Clinic : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? AddressCountry { get; set; }
        public string? AddressGovernRate { get; set; }
        public string? AddressCity { get; set; }
        public string? AddressLocation { get; set; }
        public string? Phone { get; set; }
        public string Status { get; set; }
        public double Price { get; set; }
        public string? LogoPath { get; set; }
        public virtual ICollection<ClincAdminProfile> ClincAdmin { get; set; } = new HashSet<ClincAdminProfile>();
        public virtual ICollection<ClincDoctorProfile> ClincDoctor { get; set; } = new HashSet<ClincDoctorProfile>();
        public virtual ICollection<ClincReceptionistProfile> ClincReceptionis { get; set; } = new HashSet<ClincReceptionistProfile>();
        public string? MedicalSpecialtyId { get; set; }
        public virtual MedicalSpecialty? MedicalSpecialty { get; set; }
        public virtual ICollection<ClinicSchedule> ClinicSchedules { get; set; } = new List<ClinicSchedule>();

    }
}
