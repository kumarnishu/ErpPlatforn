using System.Linq.Expressions;

namespace Platform.Application.Abstractions;

// Defines database operations without exposing EF Core to application services.
/// <summary>Persistence boundary used by Platform services; queries stay in SQL through expression predicates.</summary>
public interface IPlatformRepository
{
    /// <summary>Returns one page of matching rows without tracking them for changes.</summary>
    Task<IReadOnlyList<TEntity>> ListAsync<TEntity>(
        Expression<Func<TEntity, bool>>? predicate,
        int skip,
        int take,
        CancellationToken cancellationToken = default) where TEntity : class;

    /// <summary>Finds one matching row and keeps it tracked so the service can update it.</summary>
    Task<TEntity?> FindAsync<TEntity>(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default) where TEntity : class;

    /// <summary>Checks for a matching row without loading it.</summary>
    Task<bool> ExistsAsync<TEntity>(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default) where TEntity : class;

    /// <summary>Adds a new entity to the current unit of work.</summary>
    Task AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class;
    Task AddRangeAsync<TEntity>(IReadOnlyCollection<TEntity> entities, CancellationToken cancellationToken = default) where TEntity : class;
    void Update<TEntity>(TEntity entity) where TEntity : class;
    /// <summary>Commits all changes in the current unit of work in one database save.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>Creates and verifies password hashes without exposing cryptographic details to use cases.</summary>
public interface IPasswordCredentialService
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}

/// <summary>Issues short-lived access tokens for a user's selected tenant membership.</summary>
public interface IPlatformTokenIssuer
{
    string Issue(Platform.Domain.PlatformUser user, int tenantId, Platform.Domain.TenantMembershipRole role);
}
