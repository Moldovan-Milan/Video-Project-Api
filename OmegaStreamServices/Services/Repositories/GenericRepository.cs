using Microsoft.EntityFrameworkCore;
using OmegaStreamServices.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services.Repositories
{
    /// <summary>
    /// Generic repository class that provides basic CRUD operations for entities of type T.
    /// </summary>
    public class GenericRepository : IGenericRepository
    {
        private readonly AppDbContext _context;

        public GenericRepository(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Asynchronously adds an entity of type T to the database.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the entity to add. This should be a class that represents a table in the database.
        /// </typeparam>
        /// <param name="entity">
        /// The entity to add to the database. This should be an instance of the class that represents the table.
        /// </param>
        /// <returns></returns>
        public async Task AddAsync<T>(T entity) where T : class
        {
            await _context.Set<T>().AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Asynchronously checks if any entity of type T exists in the database that matches the specified predicate.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the entity to check. This should be a class that represents a table in the database.
        /// </typeparam>
        /// <param name="predicate">
        /// An expression that defines the conditions for which entity to check. This allows you to specify criteria for filtering the results.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a boolean value indicating whether any entity of type T exists that matches the specified predicate.
        /// </returns>
        public async Task<bool> AnyAsync<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return await _context.Set<T>().AnyAsync(predicate);
        }

        /// <summary>
        /// Asynchronously counts the number of entities of type T in the database that match the specified predicate.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the entity to count. This should be a class that represents a table in the database.
        /// </typeparam>
        /// <param name="predicate">
        /// An expression that defines the conditions for which entity to count. This allows you to specify criteria for filtering the results.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the number of entities of type T that match the specified predicate.
        /// </returns>
        public Task<int> CountAsync<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return _context.Set<T>().CountAsync(predicate);
        }

        /// <summary>
        /// Asynchronously deletes an entity of type T from the database.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the entity to delete. This should be a class that represents a table in the database.
        /// </typeparam>
        /// <param name="entity">
        /// The entity to delete from the database. This should be an instance of the class that represents the table.
        /// </param>
        /// <returns></returns>
        public async Task DeleteAsync<T>(T entity) where T : class
        {
            _context.Set<T>().Remove(entity);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Get the first entity of type T from the database that matches the specified predicate, including related entities if specified.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the entity to retrieve. This should be a class that represents a table in the database.
        /// </typeparam>
        /// <param name="predicate">
        /// An expression that defines the conditions for which entity to retrieve. This allows you to specify criteria for filtering the results.
        /// </param>
        /// <param name="include">
        /// An optional function to include related entities in the query. This allows you to specify which related entities to load along with the main entity.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the first entity of type T that matches the specified predicate, or null if no such entity exists.
        /// </returns>
        public async Task<T?> FirstOrDefaultAsync<T>(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IQueryable<T>>? include = null) where T : class
        {
            IQueryable<T> query = _context.Set<T>().Where(predicate);
            if (include != null)
            {
                query = include(query);
            }
            return await query.FirstOrDefaultAsync();
        }

        /// <summary>
        /// Get all entities of type T from the database with optional filtering, including related entities, and pagination.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the entity to retrieve. This should be a class that represents a table in the database.
        /// </typeparam>
        /// <param name="filter">
        /// An optional filter expression to apply to the query. This allows you to specify conditions for which entities to retrieve.
        /// </param>
        /// <param name="include">
        /// An optional function to include related entities in the query. This allows you to specify which related entities to load along with the main entity.
        /// </param>
        /// <param name="page">
        /// An optional page number for pagination. If provided, the method will return only the entities on that page.
        /// </param>
        /// <param name="pageSize">
        /// An optional page size for pagination. This specifies how many entities to return per page.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a list of entities of type T that match the specified criteria.
        /// </returns>
        public async Task<List<T>> GetAllAsync<T>(Expression<Func<T, bool>>? filter = null, Func<IQueryable<T>, IQueryable<T>>? include = null, int? page = null, int? pageSize = null) where T : class
        {
            IQueryable<T> query = _context.Set<T>();
            
            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (include != null)
            {
                query = include(query);
            }

            if (page.HasValue && pageSize.HasValue)
            {
                query = query.Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value);
            }

            return await query.ToListAsync();
        }

        /// <summary>
        /// Asynchronously updates an entity of type T in the database.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the entity to update. This should be a class that represents a table in the database.
        /// </typeparam>
        /// <param name="entity">
        /// The entity to update in the database. This should be an instance of the class that represents the table.
        /// </param>
        /// <returns></returns>
        public async Task UpdateAsync<T>(T entity) where T : class
        {
            _context.Set<T>().Update(entity);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Asynchronously retrieves a list of entities of type TResult from the database that match the specified predicate.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the entity to retrieve. This should be a class that represents a table in the database.
        /// </typeparam>
        /// <typeparam name="TResult">
        /// The type of the result to select. This should be a class that represents the shape of the data you want to retrieve.
        /// </typeparam>
        /// <param name="predicate">
        /// An expression that defines the conditions for which entity to retrieve. This allows you to specify criteria for filtering the results.
        /// </param>
        /// <param name="selector">
        /// An expression that defines the shape of the data to select. This allows you to specify which properties of the entity to include in the result.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a list of entities of type TResult that match the specified predicate.
        /// </returns>
        public Task<List<TResult>> WhereAsync<T, TResult>(Expression<Func<T, bool>> predicate, Expression<Func<T, TResult>> selector) where T : class
        {
            return _context.Set<T>().Where(predicate).Select(selector).ToListAsync();
        }

        /// <summary>
        /// Asynchronously retrieves a list of entities of type T from the database that match the specified predicate.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the entity to retrieve. This should be a class that represents a table in the database.
        /// </typeparam>
        /// <param name="predicate">
        /// An expression that defines the conditions for which entity to retrieve. This allows you to specify criteria for filtering the results.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains a list of entities of type T that match the specified predicate.
        /// </returns>
        public async Task<List<T>> WhereAsync<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return await _context.Set<T>().Where(predicate).ToListAsync();
        }

        /// <summary>
        /// Asynchronously retrieves an entity of type T from the database using the specified keys.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the entity to retrieve. This should be a class that represents a table in the database.
        /// </typeparam>
        /// <param name="keys">
        /// An array of keys to use for finding the entity. This should match the primary key(s) of the entity.
        /// </param>
        /// <returns>
        /// A task that represents the asynchronous operation. The task result contains the entity of type T that matches the specified keys, or null if no such entity exists.
        /// </returns>
        public async Task<T?> FindWithKeysAsync<T>(params object[] keys) where T : class
        {
            return await _context.Set<T>().FindAsync(keys);
        }

        /// <summary>
        /// Asynchronously deletes multiple entities of type T from the database.
        /// </summary>
        /// <typeparam name="T">
        /// The type of the entity to delete. This should be a class that represents a table in the database.
        /// </typeparam>
        /// <param name="entities">
        /// A list of entities to delete from the database. This should be a list of instances of the class that represents the table.
        /// </param>
        /// <returns></returns>
        public async Task DeleteMultipleAsync<T>(List<T> entities) where T : class
        {
            _context.Set<T>().RemoveRange(entities);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteMultipleAsync<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            var entites = await _context.Set<T>().Where(predicate).ToListAsync();
            if (entites != null && entites.Count > 0)
            {
                _context.Set<T>().RemoveRange(entites);
                await _context.SaveChangesAsync();
            }
        }
    }
}
