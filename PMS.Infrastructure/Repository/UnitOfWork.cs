using PMS.Application.Abstractions.Data;
using PMS.Infrastructure.Database;

namespace PMS.Infrastructure.Repository;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _dataContext;

    public UnitOfWork(ApplicationDbContext dataContext)
    {
        _dataContext = dataContext;
    }

    public int Commit()
    {
        return _dataContext.SaveChanges();
    }

    public async Task<int> CommitAsync(CancellationToken cancellationToken)
    {
        return await _dataContext.SaveChangesAsync(cancellationToken);
    }

    public void Rollback()
    {
        _dataContext.Dispose();
    }

    public async Task RollbackAsync()
    {
        await _dataContext.DisposeAsync();
    }
}
