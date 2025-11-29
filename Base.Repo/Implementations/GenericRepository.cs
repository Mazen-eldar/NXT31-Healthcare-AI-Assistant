using Microsoft.EntityFrameworkCore;
using Base.Repo.Interfaces;
using Base.Repo.Specifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Base.DAL.Contexts;
using Base.DAL.Models.BaseModels;

namespace Base.Repo.Implementations
{
    public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
    {
        private readonly DbSet<T> _dbSet;
        private readonly AppDbContext _context;

        public GenericRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        // ---------------------- Write Operations ----------------------
        public async Task<T> AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            return entity;
        }

        public async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
        {
            if (entities == null || !entities.Any())
                return Enumerable.Empty<T>();

            await _dbSet.AddRangeAsync(entities);
            return entities;
        }

        public Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(T entity)
        {
            _dbSet.Remove(entity);
            return Task.CompletedTask;
        }
        public async Task RemoveRangeAsync(IEnumerable<T> entities)
        {
            if (entities == null || !entities.Any())
                return;

             _dbSet.RemoveRange(entities);
             await Task.CompletedTask;
        }

        // ---------------------- Read Operations ----------------------

        public async Task<T> GetByIdAsync(string id, bool asNoTracking = false)
        {
            IQueryable<T> query = _dbSet;
            if (asNoTracking)
                query = query.AsNoTracking();

            return await query.FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<IReadOnlyList<T>> ListAllAsync(bool asNoTracking = false)
        {
            IQueryable<T> query = _dbSet;
            if (asNoTracking)
                query = query.AsNoTracking();

            return await query.ToListAsync();
        }

        // ---------------------- Specification ----------------------

        private IQueryable<T> ApplySpecification(ISpecification<T> spec, bool asNoTracking = true)
        {
            var query = _dbSet.AsQueryable();

            if (asNoTracking)
                query = query.AsNoTracking();

            return SpecificationEvaluator<T>.GetQuery(query, spec);
        }

        public async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec, bool asNoTracking = true)
        {
            return await ApplySpecification(spec, asNoTracking).ToListAsync();
        }

        public async Task<T> GetEntityWithSpecAsync(ISpecification<T> spec, bool asNoTracking = true)
        {
            return await ApplySpecification(spec, asNoTracking).FirstOrDefaultAsync();
        }

        public async Task<int> CountAsync(ISpecification<T> spec, bool asNoTracking = true)
        {
            return await ApplySpecification(spec, asNoTracking).CountAsync();
        }
    }

    /* public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
     {
         // 💡 نستخدم DbSet مباشرة لسهولة الوصول في الدوال
         private readonly DbSet<T> _dbSet;
         private readonly AppDbContext _context;

         public GenericRepository(AppDbContext context)
         {
             _context = context;
             _dbSet = context.Set<T>();
         }

         // ----------------------------------------------------------------------------------
         // 💾 عمليات الكتابة (Write Operations) - جميعها Async
         // ----------------------------------------------------------------------------------

         // 🟢 وقائي: تم تغيير الاسم من Add إلى AddAsync، ويضمن إرجاع الكيان بعد الإضافة (للوصول للـ ID)
         public async Task<T> AddAsync(T entity)
         {
             await _dbSet.AddAsync(entity);
             // 💡 لا يتم استدعاء SaveChangesAsync هنا. يفضل تركه لـ UnitOfWork.
             return entity;
         }
         public async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities)
         {
             if (entities == null || !entities.Any())
                 return Enumerable.Empty<T>();

             await _dbSet.AddRangeAsync(entities);
             // 💡 لا يتم استدعاء SaveChangesAsync هنا. يفضل تركه لـ UnitOfWork.
             return entities;
         }
         // 🟢 وقائي: تم تغيير الاسم من Update إلى UpdateAsync.
         public Task UpdateAsync(T entity)
         {
             // استخدام Update لا يتطلب Async بشكل صارم هنا، لكنه يوحد التوقيع مع الواجهة.
             _dbSet.Update(entity);
             return Task.CompletedTask; // إرجاع مهمة مكتملة
         }

         // 🟢 وقائي: تم تغيير الاسم من Delete إلى DeleteAsync.
         public Task DeleteAsync(T entity)
         {
             // استخدام Remove لا يتطلب Async بشكل صارم هنا، لكنه يوحد التوقيع مع الواجهة.
             _dbSet.Remove(entity);
             return Task.CompletedTask; // إرجاع مهمة مكتملة
         }

         // ----------------------------------------------------------------------------------
         // 🔎 عمليات القراءة (Read Operations) - وقائية (AsNoTracking)
         // ----------------------------------------------------------------------------------

         // 🟢 وقائي: استخدام FindAsync قد يكون أسرع للبحث بالـ ID الأساسي
         public async Task<T> GetByIdAsync(string id)
         {
             // لا نحتاج AsNoTracking() هنا لأن FindAsync لا يتتبع افتراضياً (إذا كان غير موجود)
             // ولكنه يتتبع إذا كان موجوداً بالفعل في الـ DbContext.
             return await _dbSet.FindAsync(id);
         }

         // 🟢 وقائي: استخدام AsNoTracking() لزيادة كفاءة الذاكرة ومنع التتبع غير المرغوب
         public async Task<IReadOnlyList<T>> ListAllAsync()
         {
             return await _dbSet.AsNoTracking().ToListAsync();
         }

         // ----------------------------------------------------------------------------------
         // 🎯 عمليات المواصفات (Specification Operations) - أكثر وقائية
         // ----------------------------------------------------------------------------------

         // 💡 هنا نستخدم دالة مساعدة لإنشاء Query مع المواصفات
         private IQueryable<T> ApplySpecification(ISpecification<T> spec)
         {
             return SpecificationEvaluator<T>.GetQuery(_dbSet.AsNoTracking(), spec);
         }

         // 🟢 وقائي: جلب مجموعة من الكيانات باستخدام المواصفات (مع AsNoTracking())
         public async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec)
         {
             return await ApplySpecification(spec).ToListAsync();
         }

         // 🟢 وقائي: جلب كائن واحد باستخدام المواصفات (مع AsNoTracking())
         public async Task<T> GetEntityWithSpecAsync(ISpecification<T> spec)
         {
             // نستخدم FirstOrDefaultAsync بدلاً من SingleOrDefaultAsync لأنه أكثر تسامحًا
             return await ApplySpecification(spec).FirstOrDefaultAsync();
         }

         // 🟢 وقائي: جلب عدد الكيانات باستخدام المواصفات
         public async Task<int> CountAsync(ISpecification<T> spec)
         {
             return await ApplySpecification(spec).CountAsync();
         }
     }*/

}
