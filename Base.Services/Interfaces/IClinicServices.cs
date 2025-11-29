using Base.DAL.Models.SystemModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Base.Services.Interfaces
{
    public interface IClinicServices
    {
        Task<Clinic> GetClinicAsync(Expression<Func<Clinic, bool>> CriteriaExpression, bool astracking = false);
    }
}
