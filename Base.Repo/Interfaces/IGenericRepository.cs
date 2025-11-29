using Base.Repo.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Base.DAL.Models.BaseModels;

namespace Base.Repo.Interfaces
{
    public interface IGenericRepository<T> where T : BaseEntity
    {

        // ------------------- عمليات الكتابة (Async) --------------------
        Task<T> AddAsync(T entity);
        Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities);

        Task UpdateAsync(T entity);
        Task DeleteAsync(T entity);
        Task RemoveRangeAsync(IEnumerable<T> entities);
        // -------------------- عمليات القراءة -----------------------
        Task<T> GetByIdAsync(string id, bool asNoTracking = false);
        Task<IReadOnlyList<T>> ListAllAsync(bool asNoTracking = false);

        // ------------------- عمليات المواصفات ------------------
        Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec, bool asNoTracking = false);
        Task<T> GetEntityWithSpecAsync(ISpecification<T> spec, bool asNoTracking = false);

        // 🟢 وقائي: CountAsync للتأكد من أنها غير متزامنة
        Task<int> CountAsync(ISpecification<T> spec, bool asNoTracking = true);
    }
}
