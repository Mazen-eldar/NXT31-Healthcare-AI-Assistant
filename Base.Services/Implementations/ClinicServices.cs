using Base.DAL.Models.BaseModels;
using Base.DAL.Models.SystemModels;
using Base.Repo.Interfaces;
using Base.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RepositoryProject.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Base.Services.Implementations
{
    public class ClinicServices: IClinicServices
    {
        private readonly IUnitOfWork _unitOfWork;

        public ClinicServices(UserManager<ApplicationUser> userManager, IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public  async Task<Clinic> GetClinicAsync(Expression<Func<Clinic, bool>> CriteriaExpression, bool asnotracking = false)
        {
            var ClinicRepo = _unitOfWork.Repository<Clinic>();
            var spec = new BaseSpecification<Clinic>(CriteriaExpression);
            spec.AllIncludes.Add(c => c.Include(_c => _c.MedicalSpecialty));
            var clinic = await ClinicRepo.GetEntityWithSpecAsync(spec, asnotracking);
            return clinic;
        }
    }
}
