using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmegaStreamServices.Services.Base
{
    public interface IBaseRepository<T> where T : class
    {
        Task Add(T entity);
        Task<List<T>> GetAll();
        Task<T> FindByIdAsync(int id);
        void Update(T entity);
        void Delete(T entity);
        Task DeleteMultipleAsync(List<T> entities);
    }
}
