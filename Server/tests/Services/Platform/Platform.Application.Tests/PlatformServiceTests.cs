using System.Linq.Expressions;
using BuildingBlocks.Context;
using BuildingBlocks.Http;
using Platform.Application.Abstractions;
using Platform.Application.Services;
using Platform.Domain;

namespace Platform.Application.Tests;

// Exercise subscription, membership, and action authorization behavior through the application service.
public sealed class PlatformServiceTests
{
    [Fact]
    public async Task Signup_requires_a_product_and_creates_owner_membership_with_subscription()
    {
        var repository = new FakePlatformRepository();
        repository.Seed(new Product { Id = 1, Key = "erp", Name = "ERP", Status = CatalogStatus.Active });
        var service = CreateService(repository, "owner-1");

        var error = await Assert.ThrowsAsync<ApiException>(() => service.CreateTenantAsync("acme", "Acme", []));
        Assert.Equal(400, error.StatusCode);

        var tenant = await service.CreateTenantAsync("acme", "Acme", [1]);

        Assert.Single(tenant.ProductSubscriptions);
        Assert.Equal(TenantProductSubscriptionStatus.Active, tenant.ProductSubscriptions.Single().Status);
        Assert.Equal("owner-1", tenant.Members.Single().UserId);
        Assert.Equal(TenantMembershipStatus.Active, tenant.Members.Single().Status);
    }

    [Fact]
    public async Task Active_tenant_cannot_lose_its_last_active_product()
    {
        var repository = CreateSubscribedTenantRepository();
        var service = CreateService(repository, "admin-1", isAdmin: true);

        var error = await Assert.ThrowsAsync<ApiException>(() => service.UpdateSubscriptionAsync(
            1, 1, TenantProductSubscriptionStatus.Cancelled, null));

        Assert.Equal(409, error.StatusCode);
        Assert.Equal(TenantProductSubscriptionStatus.Active,
            repository.Rows<TenantProductSubscription>().Single(x => x.ProductId == 1).Status);
    }

    [Fact]
    public async Task User_can_only_use_an_action_when_membership_subscription_and_assignment_are_active()
    {
        var repository = CreateSubscribedTenantRepository();
        var membership = new TenantMembership { Id = 1, TenantId = 1, UserId = "user-1", Status = TenantMembershipStatus.Active };
        var (product, module, feature, action) = CreateActionPath();
        repository.Seed(membership, product, module, feature, action,
            new TenantUserAction { Id = 1, TenantMembershipId = 1, ProductActionId = 1, Membership = membership, Action = action, IsActive = true });
        var service = CreateService(repository, "user-1");

        var allowed = await service.CheckAccessAsync(1, "erp", "invoices", "invoice-records", "export");
        Assert.True(allowed.Allowed);

        var adminService = CreateService(repository, "admin-1", isAdmin: true);
        await adminService.RevokeActionAsync(1, 1, 1);
        var denied = await service.CheckAccessAsync(1, "erp", "invoices", "invoice-records", "export");
        Assert.False(denied.Allowed);
        Assert.Equal("action_not_assigned", denied.DenialCode);
    }

    [Fact]
    public async Task Action_assignment_requires_an_active_subscription_to_its_product()
    {
        var repository = CreateSubscribedTenantRepository();
        repository.Rows<TenantProductSubscription>().Single().Status = TenantProductSubscriptionStatus.Cancelled;
        var membership = new TenantMembership { Id = 1, TenantId = 1, UserId = "user-1", Status = TenantMembershipStatus.Active };
        var (product, module, feature, action) = CreateActionPath();
        repository.Seed(membership, product, module, feature, action);
        var service = CreateService(repository, "admin-1", isAdmin: true);

        var error = await Assert.ThrowsAsync<ApiException>(() => service.AssignActionAsync(1, 1, 1));

        Assert.Equal(409, error.StatusCode);
        Assert.Empty(repository.Rows<TenantUserAction>());
    }

    [Fact]
    public async Task Catalog_writes_require_platform_admin_claim()
    {
        var service = CreateService(new FakePlatformRepository(), "user-1");

        var error = await Assert.ThrowsAsync<ApiException>(() => service.CreateCatalogItemAsync(
            new Product { Key = "erp", Name = "ERP" }));

        Assert.Equal(403, error.StatusCode);
    }

    private static PlatformService CreateService(FakePlatformRepository repository, string? userId = null, bool isAdmin = false) =>
        new(repository, new FakeUserContext(userId, userId is not null, isAdmin ? ["PlatformAdmin"] : []));

    private static FakePlatformRepository CreateSubscribedTenantRepository()
    {
        var repository = new FakePlatformRepository();
        repository.Seed(
            new Tenant { Id = 1, Key = "acme", Name = "Acme", Status = TenantStatus.Active },
            new TenantProductSubscription { Id = 1, TenantId = 1, ProductId = 1, Status = TenantProductSubscriptionStatus.Active });
        return repository;
    }

    private static (Product Product, ProductModule Module, ProductFeature Feature, ProductAction Action) CreateActionPath()
    {
        var product = new Product { Id = 1, Key = "erp", Name = "ERP", Status = CatalogStatus.Active };
        var module = new ProductModule { Id = 1, ProductId = 1, Key = "invoices", Name = "Invoices", Status = CatalogStatus.Active, Product = product };
        var feature = new ProductFeature { Id = 1, ProductModuleId = 1, Key = "invoice-records", Name = "Invoice records", Status = CatalogStatus.Active, Module = module };
        var action = new ProductAction { Id = 1, ProductFeatureId = 1, Key = "export", Name = "Export", Status = CatalogStatus.Active, Feature = feature };
        return (product, module, feature, action);
    }

    private sealed class FakeUserContext(string? userId, bool isAuthenticated, IReadOnlyCollection<string> roles) : IUserContext
    {
        public string? UserId { get; } = userId;
        public bool IsAuthenticated { get; } = isAuthenticated;
        public IReadOnlyCollection<string> Roles { get; } = roles;
    }

    private sealed class FakePlatformRepository : IPlatformRepository
    {
        private readonly List<object> _rows = [];

        public void Seed(params object[] rows) => _rows.AddRange(rows);

        public IReadOnlyList<TEntity> Rows<TEntity>() where TEntity : class => _rows.OfType<TEntity>().ToArray();

        public Task<IReadOnlyList<TEntity>> ListAsync<TEntity>(Expression<Func<TEntity, bool>>? predicate, int skip, int take, CancellationToken cancellationToken = default) where TEntity : class
        {
            IEnumerable<TEntity> rows = Rows<TEntity>();
            if (predicate is not null) rows = rows.Where(predicate.Compile());
            return Task.FromResult<IReadOnlyList<TEntity>>(rows.Skip(skip).Take(take).ToArray());
        }

        public Task<TEntity?> FindAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) where TEntity : class =>
            Task.FromResult(Rows<TEntity>().FirstOrDefault(predicate.Compile()));

        public Task<bool> ExistsAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) where TEntity : class =>
            Task.FromResult(Rows<TEntity>().Any(predicate.Compile()));

        public Task AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
        {
            _rows.Add(entity);
            if (entity is Tenant tenant)
            {
                _rows.AddRange(tenant.Members);
                _rows.AddRange(tenant.ProductSubscriptions);
            }
            return Task.CompletedTask;
        }

        public Task AddRangeAsync<TEntity>(IReadOnlyCollection<TEntity> entities, CancellationToken cancellationToken = default) where TEntity : class
        {
            _rows.AddRange(entities);
            return Task.CompletedTask;
        }

        public void Update<TEntity>(TEntity entity) where TEntity : class { }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Approximate SQL Server identity generation so service tests can inspect saved graphs.
            foreach (var row in _rows)
            {
                var id = row.GetType().GetProperty("Id");
                if (id?.PropertyType == typeof(int) && (int)(id.GetValue(row) ?? 0) == 0)
                    id.SetValue(row, _rows.Count(x => x.GetType() == row.GetType() && x.GetType().GetProperty("Id")?.GetValue(x) is int value && value > 0) + 1);
            }
            foreach (var tenant in Rows<Tenant>())
            {
                foreach (var membership in tenant.Members) membership.TenantId = tenant.Id;
                foreach (var subscription in tenant.ProductSubscriptions) subscription.TenantId = tenant.Id;
            }
            foreach (var assignment in Rows<TenantUserAction>())
            {
                assignment.TenantMembershipId = assignment.Membership?.Id ?? assignment.TenantMembershipId;
                assignment.ProductActionId = assignment.Action?.Id ?? assignment.ProductActionId;
            }
            return Task.CompletedTask;
        }
    }
}
