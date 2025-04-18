using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services.Repositories
{
    public interface IGenericRepository
    {
        Task<List<T>> GetAllAsync<T>(
            Expression<Func<T, bool>>? filter = null,
            Func<IQueryable<T>, IQueryable<T>>? include = null,
            int? page = null,
            int? pageSize = null) where T : class;

        Task<T?> FirstOrDefaultAsync<T>(
            Expression<Func<T, bool>> predicate,
            Func<IQueryable<T>, IQueryable<T>>? include = null) where T : class;

        Task<int> CountAsync<T>(
            Expression<Func<T, bool>> predicate) where T : class;

        Task<bool> AnyAsync<T>(
            Expression<Func<T, bool>> predicate) where T : class;

        Task<List<TResult>> WhereAsync<T, TResult>(
            Expression<Func<T, bool>> predicate,
            Expression<Func<T, TResult>> selector) where T : class;

        Task<List<T>> WhereAsync<T>(
            Expression<Func<T, bool>> predicate) where T : class;

        Task<T?> FindWithKeysAsync<T>(params object[] keys) where T : class;

        Task AddAsync<T>(T entity) where T : class;
        Task UpdateAsync<T>(T entity) where T : class;
        Task DeleteAsync<T>(T entity) where T : class;
        Task DeleteMultipleAsync<T>(List<T> entities) where T : class;
        Task DeleteMultipleAsync<T>(Expression<Func<T, bool>> predicate) where T : class;
    }
}
