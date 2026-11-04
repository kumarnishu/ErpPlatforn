using Microsoft.EntityFrameworkCore;
using Platform.Domain;

namespace Platform.Infrastructure.Persistence;

// Maps the platform access entities to their SQL Server tables and enforces their key relationships.
/// <summary>EF Core context for tenant subscriptions and product feature permissions.</summary>
public class PlatformDbContext(DbContextOptions<PlatformDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<PlatformUser> PlatformUsers => Set<PlatformUser>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductModule> ProductModules => Set<ProductModule>();
    public DbSet<ProductFeature> ProductFeatures => Set<ProductFeature>();
    public DbSet<ProductAction> ProductActions => Set<ProductAction>();
    public DbSet<TenantProductSubscription> TenantProductSubscriptions => Set<TenantProductSubscription>();
    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();
    public DbSet<TenantUserAction> TenantUserActions => Set<TenantUserAction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PlatformUser>(entity =>
        {
            entity.ToTable("PlatformUsers");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
            entity.Property(x => x.NormalizedEmail).HasMaxLength(320).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            entity.HasIndex(x => x.UserId).IsUnique();
            entity.HasIndex(x => x.NormalizedEmail).IsUnique();
        });

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("Tenants");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Key).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.ConcurrencyStamp).IsConcurrencyToken();
            entity.HasIndex(x => x.Key).IsUnique();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Key).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.HasIndex(x => x.Key).IsUnique();
        });

        modelBuilder.Entity<ProductModule>(entity =>
        {
            entity.ToTable("ProductModules");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Key).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.HasIndex(x => new { x.ProductId, x.Key }).IsUnique();
            entity.HasOne(x => x.Product).WithMany(x => x.Modules)
                .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductFeature>(entity =>
        {
            entity.ToTable("ProductFeatures");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Key).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.HasIndex(x => new { x.ProductModuleId, x.Key }).IsUnique();
            entity.HasOne(x => x.Module).WithMany(x => x.Features)
                .HasForeignKey(x => x.ProductModuleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductAction>(entity =>
        {
            entity.ToTable("ProductActions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Key).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.HasIndex(x => new { x.ProductFeatureId, x.Key }).IsUnique();
            entity.HasOne(x => x.Feature).WithMany(x => x.Actions)
                .HasForeignKey(x => x.ProductFeatureId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TenantProductSubscription>(entity =>
        {
            entity.ToTable("TenantProductSubscriptions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.HasIndex(x => new { x.TenantId, x.ProductId }).IsUnique();
            entity.HasOne(x => x.Tenant).WithMany(x => x.ProductSubscriptions)
                .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Product).WithMany(x => x.TenantSubscriptions)
                .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TenantMembership>(entity =>
        {
            entity.ToTable("TenantMemberships");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserId).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.HasIndex(x => new { x.TenantId, x.UserId }).IsUnique();
            entity.HasOne(x => x.Tenant).WithMany(x => x.Members)
                .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TenantUserAction>(entity =>
        {
            entity.ToTable("TenantUserActions");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantMembershipId, x.ProductActionId }).IsUnique();
            entity.HasOne(x => x.Membership).WithMany(x => x.ActionAssignments)
                .HasForeignKey(x => x.TenantMembershipId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Action).WithMany(x => x.UserAssignments)
                .HasForeignKey(x => x.ProductActionId).OnDelete(DeleteBehavior.Restrict);
        });

        // The signup catalog is ready to use immediately after applying the initial migration.
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Key = "erp", Name = "ERP", Status = CatalogStatus.Active },
            new Product { Id = 2, Key = "checklist", Name = "Checklist", Status = CatalogStatus.Active },
            new Product { Id = 3, Key = "data-studio", Name = "Data Studio", Status = CatalogStatus.Active },
            new Product { Id = 4, Key = "hrms", Name = "HRMS", Status = CatalogStatus.Active });
    }
}
