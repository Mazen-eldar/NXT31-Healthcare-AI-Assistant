using Base.DAL.Models.BaseModels;
using Base.Repo.Specifications;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RepositoryProject.Specifications
{
    public class BaseSpecification<T> : ISpecification<T> where T : BaseEntity
    {
        public Expression<Func<T, bool>> Criteria { get; set; }
        public List<Expression<Func<T, object>>> Includes { get; set; } = new List<Expression<Func<T, object>>>();
        public Expression<Func<T, object>> OrderBy { get; set; }
        public Expression<Func<T, object>> OrderByDescending { get; set; }

        public List<Func<IQueryable<T>, IIncludableQueryable<T, object>>> AllIncludes { get; set; } = new List<Func<IQueryable<T>, IIncludableQueryable<T, object>>>();

        public int Skip { get; private set; }
        public int Take { get; private set; }
        public bool IsPagingEnabled { get; private set; }

        public BaseSpecification()
        {
            //Includes = new List<Expression<Func<T, object>>>();
        }
        public BaseSpecification(Expression<Func<T, bool>> CriteriaExpression)
        {
            Criteria = CriteriaExpression;
            //Includes = new List<Expression<Func<T, object>>>();
        }

        public void AddOrderBy(Expression<Func<T, object>> orderByExpression)
        {
            OrderBy = orderByExpression;
        }

        public void AddOrderByDesc(Expression<Func<T, object>> orderByDescExpression)
        {
            OrderByDescending = orderByDescExpression;
        }
        public void ApplyPaging(int skip, int take)
        {
            Skip = skip;
            Take = take;
            IsPagingEnabled = true;
        }
    }
}
