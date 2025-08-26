using backend.Context;
using backend.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace backend.UOW;

public class UnitOfWork : IUnitOfWork 
{
    private readonly DbContextBrainlyPhysio _context;
    private readonly Dictionary<Type, object> _repositories;

    public UnitOfWork(DbContextBrainlyPhysio context)
    {
        _context = context;
        _repositories = new Dictionary<Type, object>();
    }

    public IGenericRepository<TEntity> Repository<TEntity>() where TEntity : class
    {
        if (_repositories.ContainsKey(typeof(TEntity)))
        {
            return (IGenericRepository<TEntity>)_repositories[typeof(TEntity)];
        }

        var repository = new GenericRepository<TEntity>(_context);
        _repositories[typeof(TEntity)] = repository;
        return repository;
        // return new GenericRepository<TEntity>(_context);
    }
    
    

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync(IDbContextTransaction transaction)
    {
        try
        {
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task RollBackTransactionAsync(IDbContextTransaction transaction)
    {
        await transaction.RollbackAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }
}