using System.Linq.Expressions;

namespace TestPlatform.Data.Repositories;

public interface IRepository<TEntity> where TEntity : class
{
    IQueryable<TEntity> GetAll(Expression<Func<TEntity, bool>>? expression = null, bool isTracking = true);
    Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> expression, bool isTracking = true);
    Task<TEntity> AddAsync(TEntity entity);
    TEntity Update(TEntity entity);
    bool Delete(TEntity entity);
    Task<bool> SaveChangesAsync();
}