using Microsoft.EntityFrameworkCore;
using Platform.Application.Abstractions;
using Platform.Domain;

namespace Platform.Infrastructure.Persistence;

public sealed class TenantSignupRepository(PlatformDbContext db) : ITenantSignupRepository
{
    public Task<bool> TenantKeyExistsAsync(string key, CancellationToken cancellationToken) =>
        db.Tenants.AnyAsync(x => x.Key == key, cancellationToken);

    public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        db.Users.AnyAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);

    public async Task AddTenantWithAdminAsync(
        Tenant tenant,
        User user,
        TenantMembership membership,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            db.Tenants.Add(tenant);
            db.Users.Add(user);
            db.Memberships.Add(membership);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            if (await TenantKeyExistsAsync(tenant.Key, cancellationToken))
                throw new TenantSignupConflictException("tenant-key");
            if (await EmailExistsAsync(user.NormalizedEmail, cancellationToken))
                throw new TenantSignupConflictException("email");
            throw;
        }
    }
}
