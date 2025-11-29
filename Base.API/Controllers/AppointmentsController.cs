using Base.DAL.Models.SystemModels;
using Base.Repo.Interfaces;
using Base.Services.Implementations;
using Base.Services.Interfaces;
using Base.Shared.Responses.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RepositoryProject.Specifications;
using System.Security.Claims;

namespace Base.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(Policy = "ActiveUserOnly")] // Restrict to active users; adjust roles if needed, e.g., [Authorize(Roles = "Patient")]
    public class AppointmentsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IUserProfileService _userProfileService; // If needed for patient profile
        private readonly IUploadImageService _uploadImageService;

        public AppointmentsController(IUnitOfWork unitOfWork, IUserProfileService userProfileService, IUploadImageService uploadImageService)
        {
            _unitOfWork = unitOfWork;
            _userProfileService = userProfileService;
            _uploadImageService = uploadImageService;
        }

        /// <summary>
        /// Get all clinics
        /// </summary>
        /// <remarks>
        /// Returns a list of all active clinics with basic details.
        /// </remarks>
        /// <response code="200">List of clinics</response>
        /// <response code="404">No clinics found</response>
        [HttpGet("clinics")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllClinics([FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var clinicRepo = _unitOfWork.Repository<Clinic>();
            var spec = new BaseSpecification<Clinic>(c => (string.IsNullOrEmpty(search) || c.Name.Contains(search)) && c.Status == "Active"); // Assuming Status is string
            spec.ApplyPaging(page, pageSize);
            spec.AllIncludes.Add(c => c.Include(_c => _c.MedicalSpecialty));

            var clinics = await clinicRepo.ListAsync(spec);
            if (!clinics.Any())
                throw new NotFoundException("No clinics found.");

            var result = clinics.Select(c => new ClinicResponseDTO
            {
                Id = c.Id,
                Name = c.Name,
                Email = c.Email,
                Address = $"{c.AddressLocation}, {c.AddressCity}, {c.AddressGovernRate}, {c.AddressCountry}",
                Phone = c.Phone,
                Price = c.Price,
                LogoPath = string.IsNullOrEmpty(c.LogoPath)
                                                   ? null
                                                   : _uploadImageService.GetImageAsync(c.LogoPath).Result,
                MedicalSpecialty = c.MedicalSpecialty?.Name
            });

            return Ok(result);
        }

        /// <summary>
        /// Get all medical specialties
        /// </summary>
        /// <remarks>
        /// Returns a list of all medical specialties.
        /// </remarks>
        /// <response code="200">List of specialties</response>
        /// <response code="404">No specialties found</response>
        [HttpGet("specialties")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllSpecialties()
        {
            var specialtyRepo = _unitOfWork.Repository<MedicalSpecialty>();
            var specialties = await specialtyRepo.ListAllAsync();
            if (!specialties.Any())
                throw new NotFoundException("No medical specialties found.");

            var result = specialties.Select(s => new SpecialtyResponseDTO
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description
            });

            return Ok(result);
        }

        /// <summary>
        /// Get all clinics in specific specialty
        /// </summary>
        /// <remarks>
        /// Returns a list of all active clinics with basic details.
        /// </remarks>
        /// <response code="200">List of clinics</response>
        /// <response code="404">No clinics found</response>
        [HttpGet("specialty/clinics")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllClinicsInSpecialty([FromQuery] string SpecialtyId, [FromQuery] string? search = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var clinicRepo = _unitOfWork.Repository<Clinic>();
            var spec = new BaseSpecification<Clinic>(c => (string.IsNullOrEmpty(search) || c.Name.Contains(search)) && c.MedicalSpecialtyId == SpecialtyId && c.Status == "Active"); // Assuming Status is string
            spec.ApplyPaging(page, pageSize);
            spec.AllIncludes.Add(c => c.Include(_c => _c.MedicalSpecialty));

            var clinics = await clinicRepo.ListAsync(spec);
            if (!clinics.Any())
                throw new NotFoundException("No clinics found.");

            var result = clinics.Select(c => new ClinicResponseDTO
            {
                Id = c.Id,
                Name = c.Name,
                Email = c.Email,
                Address = $"{c.AddressLocation}, {c.AddressCity}, {c.AddressGovernRate}, {c.AddressCountry}",
                Phone = c.Phone,
                Price = c.Price,
                LogoPath = string.IsNullOrEmpty(c.LogoPath)
                                                   ? null
                                                   : _uploadImageService.GetImageAsync(c.LogoPath).Result,
                MedicalSpecialty = c.MedicalSpecialty?.Name
            });

            return Ok(result);
        }


        /// <summary>
        /// Get doctors in a specific clinic
        /// </summary>
        /// <remarks>
        /// Returns a list of doctors associated with the given clinic.
        /// </remarks>
        /// <param name="clinicId">The ID of the clinic</param>
        /// <response code="200">List of doctors</response>
        /// <response code="400">Invalid clinic ID</response>
        /// <response code="404">No doctors found in the clinic</response>
        [HttpGet("doctors/{clinicId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetDoctorsInClinic(string clinicId)
        {
            if (string.IsNullOrWhiteSpace(clinicId))
                throw new BadRequestException("Clinic ID is required.");

            var doctorProfileRepo = _unitOfWork.Repository<ClincDoctorProfile>();
            var spec = new BaseSpecification<ClincDoctorProfile>(d => d.ClincId == clinicId);
            spec.AllIncludes.Add(c => 
            c.Include(_c => _c.User)
            .Include(_c => _c.Clinc)
            );

            var doctors = await doctorProfileRepo.ListAsync(spec);
            if (!doctors.Any())
                throw new NotFoundException($"No doctors found in clinic with ID {clinicId}.");

            var result = doctors.Select(d => new DoctorResponseDTO
            {
                UserId = d.UserId,
                FullName = d.User?.FullName,
                Email = d.User?.Email,
                ClinicName = d.Clinc?.Name
            });

            return Ok(result);
        }

        /// <summary>
        /// Get available slots for a doctor on a specific date
        /// </summary>
        /// <remarks>
        /// Returns available appointment slots for the doctor on the given date.
        /// </remarks>
        /// <param name="doctorId">The ID of the doctor</param>
        /// <param name="date">The date to check availability (YYYY-MM-DD)</param>
        /// <response code="200">List of available slots</response>
        /// <response code="400">Invalid doctor ID or date</response>
        /// <response code="404">No available slots found</response>
        [HttpGet("available-slots/{doctorId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAvailableSlots(string doctorId, [FromQuery] DateTime date)
        {
            if (string.IsNullOrWhiteSpace(doctorId))
                throw new BadRequestException("Doctor ID is required.");

            var scheduleRepo = _unitOfWork.Repository<AppointmentSlot>();
            var spec = new BaseSpecification<AppointmentSlot>(s => s.DoctorSchedule.Doctor.UserId == doctorId 
            && s.Date.Date >= DateTime.Now.Date
            && ((date.Date != default && s.Date.Date == date.Date) || date.Date == default)
            && !s.IsBooked);
            spec.AllIncludes.Add(e => e.Include(d => d.DoctorSchedule).ThenInclude(d => d.Doctor));
            spec.AddOrderBy(s => s.StartTime);
            var bookedSlots = await scheduleRepo.ListAsync(spec);

            if (!bookedSlots.Any())
                throw new NotFoundException($"No available slots found for doctor {doctorId} on {date:yyyy-MM-dd}.");

            var slots = bookedSlots.Select(s => new AvailableSlotDTO
            {
                SlotId = s.Id,
                Date = s.Date,
                StartTime = s.StartTime,
                EndTime = s.EndTime
            }).ToList();

            return Ok(slots);
            //throw new NotFoundException($"No booked slots found for doctor {doctorId} on {date:yyyy-MM-dd}.");
            //return Ok(bookedSlots);
        }

        /// <summary>
        /// Book an appointment
        /// </summary>
        /// <remarks>
        /// Books an appointment for the current user (patient) in the specified slot.
        /// </remarks>
        /// <param name="request">Booking request details</param>
        /// <response code="201">Appointment booked successfully</response>
        /// <response code="400">Invalid request or slot not available</response>
        [HttpPost("book")]
        [Authorize]
        public async Task<IActionResult> BookAppointment([FromBody] BookAppointmentDTO request)
        {
            if (!ModelState.IsValid)
                throw new BadRequestException(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));

            await using var transaction = await _unitOfWork.BeginTransactionAsync();

            try
            {
                var slotRepo = _unitOfWork.Repository<AppointmentSlot>();
                var slotSpec = new BaseSpecification<AppointmentSlot>(s => s.Id == request.SlotId);
                var slot = await slotRepo.GetEntityWithSpecAsync(slotSpec);

                if (slot == null)
                    throw new NotFoundException("Slot not found.");
                if (slot.IsBooked) // Assuming IsBooked property
                    throw new BadRequestException("Slot is already booked.");

                var patientId = User.FindFirstValue(ClaimTypes.NameIdentifier); // Current user as patient
                if (string.IsNullOrEmpty(patientId))
                    throw new UnauthorizedException("User not authenticated.");

                var appointment = new Appointment
                {
                    SlotId = request.SlotId,
                    PatientId = patientId,
                    Reason = request.Reason
                };

                var appointmentRepo = _unitOfWork.Repository<Appointment>();
                await appointmentRepo.AddAsync(appointment);

                slot.IsBooked = true; // Mark as booked
                await slotRepo.UpdateAsync(slot);

                await _unitOfWork.CompleteAsync();
                await transaction.CommitAsync();

                return CreatedAtAction(nameof(GetAppointmentById), new { id = appointment.Id }, new { Message = "Appointment booked successfully.", AppointmentId = appointment.Id });
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Get appointment by ID (optional, for confirmation)
        /// </summary>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetAppointmentById(string id)
        {
            var appointmentRepo = _unitOfWork.Repository<Appointment>();
            var spec = new BaseSpecification<Appointment>(a => a.Id == id);
            spec.AllIncludes.Add(a => a.Include(a => a.Slot).Include(a => a.Patient));
            var appointment = await appointmentRepo.GetEntityWithSpecAsync(spec, true);
            if (appointment == null)
                throw new NotFoundException("Appointment not found.");

            return Ok(appointment);
        }
    }

    // DTOs for Responses and Requests
    public class ClinicResponseDTO
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public double Price { get; set; }
        public string? LogoPath { get; set; }
        public string MedicalSpecialty { get; set; }
    }

    public class SpecialtyResponseDTO
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class DoctorResponseDTO
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string ClinicName { get; set; }
    }

    public class AvailableSlotDTO
    {
        public string SlotId { get; set; }
        public DateTime Date { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
    }

    public class BookAppointmentDTO
    {
        public string SlotId { get; set; }
        public string Reason { get; set; }
    }
}
