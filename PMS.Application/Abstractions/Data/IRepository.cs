using System.Data.Common;

namespace PMS.Application.Abstractions.Data;

public interface IRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default); //not recommended for large datasets or any query that could return a large number of records. use directly the IApplicationDbContext instead for such cases.
    Task<T?> GetByIdAsync(object id, CancellationToken cancellationToken = default); //not recommended for large datasets or any query that could return a large number of records. use directly the IApplicationDbContext instead for such cases.
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default); 
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
    IEnumerable<T> ExecuteQuery(string query, DbParameter[] dbParams);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    void RemoveRange(IEnumerable<T> entities);
}
