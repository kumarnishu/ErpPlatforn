using Microsoft.EntityFrameworkCore;
using Platform.Api.Interfaces;
using Platform.Api.Models;

namespace Platform.Api.Data;

public sealed class PlatformDbContext(DbContextOptions<PlatformDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<PlatformUser> Users => Set<PlatformUser>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<TenantSubscription> TenantSubscriptions => Set<TenantSubscription>();
    public DbSet<PlatformFeature> PlatformFeatures => Set<PlatformFeature>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ConvertDeletesToSoftDeletes();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ConvertDeletesToSoftDeletes();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ConvertDeletesToSoftDeletes()
    {
        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>().Where(x => x.State == EntityState.Deleted))
        {
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var tenant = modelBuilder.Entity<Tenant>();
        tenant.ToTable("tenants");
        tenant.HasKey(x => x.Id);
        tenant.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid");
        tenant.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        tenant.Property(x => x.Key).HasColumnName("key").HasMaxLength(4).IsRequired();
        tenant.Property(x => x.LogoObjectName).HasColumnName("logo_object_name").HasMaxLength(1024);
        tenant.HasIndex(x => x.Key).IsUnique();
        tenant.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        tenant.Property(x => x.IsDeleted).HasColumnName("is_deleted").IsRequired();
        tenant.HasQueryFilter(x => !x.IsDeleted);
        tenant.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        tenant.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone").IsRequired();

        var user = modelBuilder.Entity<PlatformUser>();
        user.ToTable("users");
        user.HasKey(x => x.Id);
        user.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid");
        user.Property(x => x.TenantId).HasColumnName("tenant_id").HasColumnType("uuid").IsRequired();
        user.Property(x => x.Email).HasColumnName("email").HasMaxLength(320).IsRequired();
        user.Property(x => x.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(320).IsRequired();
        user.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(200).IsRequired();
        user.Property(x => x.ProfilePhotoObjectName).HasColumnName("profile_photo_object_name").HasMaxLength(1024);
        user.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(512).IsRequired();
        user.Property(x => x.Role).HasColumnName("role").HasMaxLength(64).IsRequired();
        user.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        user.Property(x => x.IsDeleted).HasColumnName("is_deleted").IsRequired();
        user.HasQueryFilter(x => !x.IsDeleted);
        user.Property(x => x.TokenVersion).HasColumnName("token_version").IsRequired();
        user.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        user.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        user.HasIndex(x => new { x.TenantId, x.NormalizedEmail }).IsUnique();
        user.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);

        var session = modelBuilder.Entity<UserSession>();
        session.ToTable("user_sessions");
        session.HasKey(x => x.Id);
        session.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid");
        session.Property(x => x.UserId).HasColumnName("user_id").HasColumnType("uuid").IsRequired();
        session.Property(x => x.TenantId).HasColumnName("tenant_id").HasColumnType("uuid").IsRequired();
        session.Property(x => x.RefreshTokenHash).HasColumnName("refresh_token_hash").HasMaxLength(64).IsRequired();
        session.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        session.Property(x => x.ExpiresAtUtc).HasColumnName("expires_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        session.Property(x => x.LastUsedAtUtc).HasColumnName("last_used_at_utc").HasColumnType("timestamp with time zone");
        session.Property(x => x.RevokedAtUtc).HasColumnName("revoked_at_utc").HasColumnType("timestamp with time zone");
        session.Property(x => x.IsDeleted).HasColumnName("is_deleted").IsRequired();
        session.HasQueryFilter(x => !x.IsDeleted);
        session.HasIndex(x => x.RefreshTokenHash).IsUnique();
        session.HasIndex(x => new { x.TenantId, x.UserId });
        session.HasIndex(x => x.ExpiresAtUtc);
        session.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        session.HasOne(x => x.Tenant).WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);

        var subscription = modelBuilder.Entity<TenantSubscription>();
        subscription.ToTable("tenant_subscriptions");
        subscription.HasKey(x => x.Id);
        subscription.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid");
        subscription.Property(x => x.TenantId).HasColumnName("tenant_id").HasColumnType("uuid").IsRequired();
        subscription.Property(x => x.PlanCode).HasColumnName("plan_code").HasMaxLength(64).IsRequired();
        subscription.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(24).IsRequired();
        subscription.Property(x => x.BillingInterval).HasColumnName("billing_interval").HasConversion<string>().HasMaxLength(16).IsRequired();
        subscription.Property(x => x.StartsAtUtc).HasColumnName("starts_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        subscription.Property(x => x.CurrentPeriodEndsAtUtc).HasColumnName("current_period_ends_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        subscription.Property(x => x.CancelledAtUtc).HasColumnName("cancelled_at_utc").HasColumnType("timestamp with time zone");
        subscription.Property(x => x.IsDeleted).HasColumnName("is_deleted").IsRequired();
        subscription.HasQueryFilter(x => !x.IsDeleted);
        subscription.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        subscription.Property(x => x.UpdatedAtUtc).HasColumnName("updated_at_utc").HasColumnType("timestamp with time zone").IsRequired();
        subscription.HasIndex(x => x.TenantId).IsUnique();
        subscription.HasOne(x => x.Tenant).WithOne(x => x.Subscription).HasForeignKey<TenantSubscription>(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);

        var feature = modelBuilder.Entity<PlatformFeature>();
        feature.ToTable("platform_features");
        feature.HasKey(x => x.Id);
        feature.Property(x => x.Id).HasColumnName("id").HasColumnType("uuid");
        feature.Property(x => x.Code).HasColumnName("code").HasMaxLength(64).IsRequired();
        feature.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        feature.Property(x => x.Price).HasColumnName("price").HasPrecision(18, 2).IsRequired();
        feature.Property(x => x.TenantSubscriptionId).HasColumnName("tenant_subscription_id").HasColumnType("uuid").IsRequired();
        feature.HasIndex(x => new { x.TenantSubscriptionId, x.Code }).IsUnique();
        feature.HasOne(x => x.TenantSubscription).WithMany(x => x.PlatformFeatures).HasForeignKey(x => x.TenantSubscriptionId).OnDelete(DeleteBehavior.Cascade);
    }
}
