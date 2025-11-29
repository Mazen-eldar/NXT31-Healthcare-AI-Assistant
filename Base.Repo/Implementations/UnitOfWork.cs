using Microsoft.EntityFrameworkCore.Storage;
using Base.Repo.Interfaces;
using Base.DAL.Contexts;
using Base.DAL.Models.BaseModels;
// ... باقي الـ usings

namespace Base.Repo.Implementations
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _dbContext;

        // 🟢 وقائي: استخدام Dictionary<Type, object> بدلاً من Hashtable (لأنه آمن للنوع)
        // يجب أن يتم تهيئته عند التصريح لتجنب Null reference
        private Dictionary<Type, object> _repositories = new Dictionary<Type, object>();

        // 💡 يجب حقن ServiceProvider إذا كان الـ GenericRepository يعتمد على DI، لكننا نستخدم الطريقة التقليدية هنا.
        public UnitOfWork(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        // 🟢 وقائي: تم تغيير الاسم ليتناسب مع الواجهة
        public async Task<int> CompleteAsync() => await _dbContext.SaveChangesAsync();

        // 🟢 وقائي: توقيع DisposeAsync سليم وفعّال
        public async ValueTask DisposeAsync() => await _dbContext.DisposeAsync();

        // ------------------------------------------------------------------
        // 🏭 مصنع المستودعات (Repository Factory)
        // ------------------------------------------------------------------

        public IGenericRepository<T> Repository<T>() where T : BaseEntity
        {
            // 🟢 وقائي: استخدام GetType() كـ Key أفضل من .Name (قد يكون هناك صنفين بنفس الاسم في namespaces مختلفة)
            var type = typeof(T);

            // 🟢 وقائي: التحقق من وجود المستودع في الذاكرة المؤقتة
            if (!_repositories.ContainsKey(type))
            {
                // 💡 ملاحظة: يجب أن يتوفر هنا GenericRepository<T> المُعدّل
                var repository = new GenericRepository<T>(_dbContext);

                // 🟢 وقائي: تخزين المستودع باستخدام الـ Type كـ Key
                _repositories.Add(type, repository);
            }

            // 🟢 وقائي: إرجاع المستودع من الذاكرة المؤقتة وإجراء عملية Casting آمنة
            // (GetOrCreate)
            return (IGenericRepository<T>)_repositories[type];
        }

        // 🟢 وقائي: توقيع المعاملات سليم
        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await _dbContext.Database.BeginTransactionAsync();
        }
    }
}