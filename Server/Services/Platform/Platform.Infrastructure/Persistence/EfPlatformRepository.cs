using System.Linq.Expressions;
using BuildingBlocks.Http;
using Microsoft.EntityFrameworkCore;
using Platform.Application.Abstractions;

namespace Platform.Infrastructure.Persistence;

// Implements the application repository against the scoped EF Core context.
/// <summary>SQL Server-backed implementation of the Platform repository.</summary>
public sealed class EfPlatformRepository(PlatformDbContext dbContext) : IPlatformRepository
{
    public async Task<IReadOnlyList<TEntity>> ListAsync<TEntity>(
        Expression<Func<TEntity, bool>>? predicate,
        int skip,
        int take,
        CancellationToken cancellationToken = default) where TEntity : class
    {
        // List results are read-only; avoiding change tracking keeps catalog and membership lists lighter.
        IQueryable<TEntity> query = dbContext.Set<TEntity>().AsNoTracking();
        if (predicate is not null)
            query = query.Where(predicate);

        return await query.Skip(skip).Take(take).ToListAsync(cancellationToken);
    }

    public Task<TEntity?> FindAsync<TEntity>(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default) where TEntity : class =>
        dbContext.Set<TEntity>().FirstOrDefaultAsync(predicate, cancellationToken);

    public Task<bool> ExistsAsync<TEntity>(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default) where TEntity : class =>
        dbContext.Set<TEntity>().AnyAsync(predicate, cancellationToken);

    public async Task AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class =>
        _ = await dbContext.Set<TEntity>().AddAsync(entity, cancellationToken);

    public Task AddRangeAsync<TEntity>(IReadOnlyCollection<TEntity> entities, CancellationToken cancellationToken = default) where TEntity : class
    {
        dbContext.Set<TEntity>().AddRange(entities);
        return Task.CompletedTask;
    }

    public void Update<TEntity>(TEntity entity) where TEntity : class => dbContext.Set<TEntity>().Update(entity);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Subscription changes touch the tenant stamp, so this also protects the last-product invariant.
            throw new ApiException(409, "CONCURRENT_UPDATE", "The tenant or access record changed during this request. Reload it and retry.");
        }
    }
}
