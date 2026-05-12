using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Gateway.Helper
{
    public interface IRepository<T>
    {
        Task<bool> AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        Task<bool> UpdateAsync(T entity);
        Task UpdateRangeAsync(IEnumerable<T> entities);
        Task<bool> DeleteAsync(T entity);
        Task DeleteRangeAsync(IEnumerable<T> entities);
        Task RemoveAsync(T entity);
        Task RemoveRangeAsync(IEnumerable<T> entities);
        Task DeleteByConditionAsync(Expression<Func<T, bool>> predicate);
    }
    
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly EFDbContext _dbContext;
        private readonly DbSet<T> _dbSet;

        public Repository(EFDbContext dbContext)
        {
            _dbContext = dbContext;
            _dbSet = _dbContext.Set<T>();
        }

        public async Task<bool> AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            var added = await _dbContext.SaveChangesAsync();
            return added > 0;
        }
        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
            await _dbContext.SaveChangesAsync();
        }
        public async Task<bool> UpdateAsync(T entity)
        {
            _dbContext.Entry(entity).State = EntityState.Modified;
            var updated = await _dbContext.SaveChangesAsync();
            return updated > 0;

        }
        public async Task UpdateRangeAsync(IEnumerable<T> entities)
        {
            _dbContext.UpdateRange(entities);
            await _dbContext.SaveChangesAsync();
        }
        public async Task<bool> DeleteAsync(T entity)
        {
            _dbContext.Entry(entity).State = EntityState.Modified;
            var deleted = await _dbContext.SaveChangesAsync();
            return deleted > 0;
        }
        public async Task DeleteRangeAsync(IEnumerable<T> entities)
        {
            _dbContext.UpdateRange(entities);
            await _dbContext.SaveChangesAsync();
        }
        public async Task RemoveAsync(T entity)
        {
            _dbSet.Remove(entity);
            await _dbContext.SaveChangesAsync();
        }
        public async Task RemoveRangeAsync(IEnumerable<T> entities)
        {
            _dbSet.RemoveRange(entities);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteByConditionAsync(Expression<Func<T, bool>> predicate)
        {
            var entitiesToDelete = _dbSet.Where(predicate);
            await entitiesToDelete.ExecuteDeleteAsync();
        }

    }
   
}
