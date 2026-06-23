using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace IMS.Core.Interfaces;

public interface IAsyncRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    void Update(T entity);
    void Delete(T entity);
    //Task<IEnumerable<object>> GetAllWithIncludesAsync(Func<object, object> value);
}
