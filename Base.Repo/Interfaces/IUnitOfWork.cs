using Microsoft.EntityFrameworkCore.Storage;
using Base.DAL.Models.BaseModels;

namespace Base.Repo.Interfaces
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        // 🟢 وقائي: استخدام IGenericRepository المُعدّلة
        IGenericRepository<T> Repository<T>() where T : BaseEntity;

        // 🟢 وقائي: يجب أن يكون إرجاع عدد الصفوف المتأثرة غير متزامن
        Task<int> CompleteAsync(); // تغيير الاسم ليتناسب مع Async

        // 🟢 وقائي: توقيع المعاملات سليم
        Task<IDbContextTransaction> BeginTransactionAsync();

    }
}
