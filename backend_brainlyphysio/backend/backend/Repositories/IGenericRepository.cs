using System.Linq.Expressions;

namespace backend.Repositories;

public interface IGenericRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(int id);

    Task<IList<TEntity>?> GetAllAsync();
    IQueryable<TEntity> FindQueryable(Expression<Func<TEntity, bool>> expression,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null);
    Task AddAsync(TEntity entity);
    Task UpdateAsync(TEntity entity);
    Task DeleteAsync(TEntity entity);
    IQueryable<TEntity> GetSimpleQueryable();

    Task<IQueryable<TEntity>?> FindQueryableOfEntitiesAsync(Expression<Func<TEntity, bool>> expression,
        params Expression<Func<TEntity, object>>[] navigationProps);
    Task DeleteRangeAsync(IList<TEntity> listOfEntities);
    Task AddRangeAsync(IList<TEntity> listOfEntities);
    Task UpdateRangeAsync(IList<TEntity> listOfEntities);
}