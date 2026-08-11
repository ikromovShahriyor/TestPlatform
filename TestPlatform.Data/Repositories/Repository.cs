using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using TestPlatform.Data.Context; // Rasmdagi Contexts papkasiga moslandi

namespace TestPlatform.Data.Repositories;

public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
{
    protected readonly AppDbContext _dbContext;
    protected readonly DbSet<TEntity> _dbSet;

    public Repository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
        _dbSet = _dbContext.Set<TEntity>();
    }

    public IQueryable<TEntity> GetAll(Expression<Func<TEntity, bool>>? expression = null, bool isTracking = true)
    {
        IQueryable<TEntity> query = _dbSet;

        if (!isTracking)
            query = query.AsNoTracking();

        if (expression != null)
            query = query.Where(expression);

        return query;
    }

    public async Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> expression, bool isTracking = true)
    {
        IQueryable<TEntity> query = _dbSet;

        if (!isTracking)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(expression);
    }

    public async Task<TEntity> AddAsync(TEntity entity)
    {
        var entry = await _dbSet.AddAsync(entity);
        return entry.Entity;
    }

    public TEntity Update(TEntity entity)
    {
        var entry = _dbSet.Update(entity);
        return entry.Entity;
    }

    public bool Delete(TEntity entity)
    {
        var entry = _dbSet.Remove(entity);
        return entry.State == EntityState.Deleted;
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await _dbContext.SaveChangesAsync() > 0;
    }
}