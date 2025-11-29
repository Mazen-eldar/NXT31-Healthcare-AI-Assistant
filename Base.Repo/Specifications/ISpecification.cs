using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Base.Repo.Specifications
{
    public interface ISpecification<T>
    {
        // شرط التصفية (مثل p => p.Price > 10)
        Expression<Func<T, bool>> Criteria { get; }

        // قائمة بالعلاقات المراد تحميلها (مثل p => p.ProductType)
        List<Expression<Func<T, object>>> Includes { get; }

        // شروط الترتيب
        Expression<Func<T, object>> OrderBy { get; }
        Expression<Func<T, object>> OrderByDescending { get; }

        // التصفح (Pagination)
        int Skip { get; }
        int Take { get; }
        bool IsPagingEnabled { get; }
    }
}
