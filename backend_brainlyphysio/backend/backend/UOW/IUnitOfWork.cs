using backend.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace backend.UOW;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<TEntity> Repository<TEntity>() where TEntity : class;
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync();
    Task CommitTransactionAsync(IDbContextTransaction dbContextTransaction);
    Task RollBackTransactionAsync(IDbContextTransaction dbContextTransaction);
    
}