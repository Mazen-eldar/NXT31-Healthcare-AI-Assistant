using Base.API.DTOs;
using Base.DAL.Models.SystemModels;
using Base.Repo.Interfaces;
using Base.Services.Implementations;
using Base.Shared.Responses.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using RepositoryProject.Specifications;
using System.ComponentModel.DataAnnotations;

namespace Base.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MedicalSpecialtyController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        public MedicalSpecialtyController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet("medical-specialties")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMedicalSpecialties()
        {
            var MedicalSpecialtyRepo = _unitOfWork.Repository<MedicalSpecialty>();
            var list = await MedicalSpecialtyRepo.ListAllAsync();
            var result = list.ToMedicalSpecialtyDTOSet();
            if (!result.Any())
            {
                throw new NotFoundException("No Medical Specialties requests are currently defined in the system.");
            }
            return Ok(new {list = result});
        }
    }

    public class MedicalSpecialtyDTO
    {
        public string? Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
    public static class MedicalSpecialtyExtensions
    {
        public static MedicalSpecialtyDTO ToMedicalSpecialtyDTO(this MedicalSpecialty Dto)
        {
            if (Dto is null)
            {
                return new MedicalSpecialtyDTO();
            }

            return new MedicalSpecialtyDTO
            {
                Id = (string.IsNullOrEmpty(Dto.Id) ? null : Dto.Id),
                Name = Dto.Name,
                Description = Dto.Description
            };
        }

        public static HashSet<MedicalSpecialtyDTO> ToMedicalSpecialtyDTOSet(this IEnumerable<MedicalSpecialty> entities)
        {
            if (entities == null)
                return new HashSet<MedicalSpecialtyDTO>();

            return entities.Select(e => e.ToMedicalSpecialtyDTO()).ToHashSet();
        }
    }
}
