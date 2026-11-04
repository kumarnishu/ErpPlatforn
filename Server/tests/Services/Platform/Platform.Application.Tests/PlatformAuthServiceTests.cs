using System.Linq.Expressions;
using BuildingBlocks.Context;
using BuildingBlocks.Http;
using Platform.Application.Abstractions;
using Platform.Application.Contracts;
using Platform.Application.Services;
using Platform.Domain;

namespace Platform.Application.Tests;

/// <summary>Checks tenant signup, tenant-scoped login, and tenant-admin-only user creation.</summary>
public sealed class PlatformAuthServiceTests
{
    [Fact]
    public async Task Registration_creates_admin_account_and_subscribes_selected_products()
    {
        var repository = new FakeRepository();
        repository.Seed(new Product { Id = 1, Key = "erp", Name = "ERP" }, new Product { Id = 2, Key = "hrms", Name = "HRMS" });
        var service = CreateService(repository, new TestUserContext(null));

        var result = await service.RegisterTenantAdminAsync(new("acme", "Acme Ltd", "Avery Admin", "admin@example.com", "long-password-123", [1, 2]));

        Assert.Equal(TenantMembershipRole.TenantAdmin, result.Membership.Role);
        Assert.Equal(result.Membership.UserId, repository.Rows<PlatformUser>().Single().UserId);
        Assert.Equal("admin@example.com", repository.Rows<PlatformUser>().Single().Email);
        Assert.Equal(2, result.Subscriptions.Count);
        Assert.All(result.Subscriptions, subscription => Assert.Equal(TenantProductSubscriptionStatus.Active, subscription.Status));
    }

    [Fact]
    public async Task Login_requires_active_membership_in_the_selected_tenant()
    {
        var repository = new FakeRepository();
        var account = new PlatformUser { UserId = "user-1", Email = "admin@example.com", NormalizedEmail = "ADMIN@EXAMPLE.COM", PasswordHash = "hashed:long-password-123" };
        repository.Seed(account,
            new Tenant { Id = 7, Key = "acme", Name = "Acme" },
            new TenantMembership { Id = 3, TenantId = 7, UserId = "user-1", Role = TenantMembershipRole.TenantAdmin },
            new TenantProductSubscription { Id = 9, TenantId = 7, ProductId = 1, Status = TenantProductSubscriptionStatus.Active });
        var tokens = new FakeTokenIssuer();
        var service = CreateService(repository, new TestUserContext(null), tokens);

        var response = await service.LoginAsync(new("admin@example.com", "long-password-123", 7));

        Assert.Equal("test-token", response.AccessToken);
        Assert.Equal(TenantMembershipRole.TenantAdmin, response.Role);
        Assert.Equal(("user-1", 7, TenantMembershipRole.TenantAdmin), tokens.LastIssued);
    }

    [Fact]
    public async Task Only_an_active_tenant_admin_can_create_a_tenant_user()
    {
        var repository = new FakeRepository();
        repository.Seed(new Tenant { Id = 7, Key = "acme", Name = "Acme", Status = TenantStatus.Active },
            new TenantMembership { Id = 1, TenantId = 7, UserId = "admin-1", Role = TenantMembershipRole.TenantAdmin, Status = TenantMembershipStatus.Active });
        var service = CreateService(repository, new TestUserContext("admin-1"));

        var created = await service.CreateTenantUserAsync(7, new("Jamie User", "jamie@example.com", "another-long-password-456"));

        Assert.Equal(TenantMembershipRole.TenantUser, created.Role);
        Assert.Equal("jamie@example.com", repository.Rows<PlatformUser>().Single().Email);
        Assert.Equal(TenantMembershipRole.TenantUser, repository.Rows<TenantMembership>().Single(x => x.UserId == created.UserId).Role);

        var forbidden = CreateService(repository, new TestUserContext("user-2"));
        var error = await Assert.ThrowsAsync<ApiException>(() => forbidden.CreateTenantUserAsync(7, new("Taylor", "taylor@example.com", "another-long-password-789")));
        Assert.Equal(403, error.StatusCode);

        var wrongTenantToken = CreateService(repository, new TestUserContext("admin-1"), tokenTenantId: "8");
        var scopeError = await Assert.ThrowsAsync<ApiException>(() => wrongTenantToken.CreateTenantUserAsync(7, new("Casey", "casey@example.com", "another-long-password-789")));
        Assert.Equal("TENANT_SCOPE_MISMATCH", scopeError.Code);
    }

    private static PlatformAuthService CreateService(FakeRepository repository, IUserContext context, FakeTokenIssuer? tokens = null, string tokenTenantId = "7") =>
        new(repository, new TestPasswordService(), tokens ?? new FakeTokenIssuer(), context, new TestTenantContext(tokenTenantId));

    private sealed class TestUserContext(string? userId) : IUserContext
    {
        public string? UserId { get; } = userId;
        public bool IsAuthenticated => UserId is not null;
        public IReadOnlyCollection<string> Roles => [];
    }

    private sealed class TestTenantContext(string? tenantId) : ITenantContext
    {
        public string? TenantId { get; } = tenantId;
    }

    private sealed class TestPasswordService : IPasswordCredentialService
    {
        public string Hash(string password) => $"hashed:{password}";
        public bool Verify(string password, string passwordHash) => Hash(password) == passwordHash;
    }

    private sealed class FakeTokenIssuer : IPlatformTokenIssuer
    {
        public (string UserId, int TenantId, TenantMembershipRole Role)? LastIssued { get; private set; }
        public string Issue(PlatformUser user, int tenantId, TenantMembershipRole role)
        {
            LastIssued = (user.UserId, tenantId, role);
            return "test-token";
        }
    }

    private sealed class FakeRepository : IPlatformRepository
    {
        private readonly List<object> rows = [];
        public void Seed(params object[] values) => rows.AddRange(values);
        public IReadOnlyList<TEntity> Rows<TEntity>() where TEntity : class => rows.OfType<TEntity>().ToArray();
        public Task<IReadOnlyList<TEntity>> ListAsync<TEntity>(Expression<Func<TEntity, bool>>? predicate, int skip, int take, CancellationToken cancellationToken = default) where TEntity : class
        {
            IEnumerable<TEntity> query = Rows<TEntity>();
            if (predicate is not null) query = query.Where(predicate.Compile());
            return Task.FromResult<IReadOnlyList<TEntity>>(query.Skip(skip).Take(take).ToArray());
        }
        public Task<TEntity?> FindAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) where TEntity : class =>
            Task.FromResult(Rows<TEntity>().FirstOrDefault(predicate.Compile()));
        public Task<bool> ExistsAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) where TEntity : class =>
            Task.FromResult(Rows<TEntity>().Any(predicate.Compile()));
        public Task AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default) where TEntity : class
        {
            rows.Add(entity);
            if (entity is Tenant tenant) { rows.AddRange(tenant.Members); rows.AddRange(tenant.ProductSubscriptions); }
            return Task.CompletedTask;
        }
        public Task AddRangeAsync<TEntity>(IReadOnlyCollection<TEntity> entities, CancellationToken cancellationToken = default) where TEntity : class { rows.AddRange(entities); return Task.CompletedTask; }
        public void Update<TEntity>(TEntity entity) where TEntity : class { }
        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var row in rows)
            {
                var id = row.GetType().GetProperty("Id");
                if (id?.PropertyType == typeof(int) && (int)(id.GetValue(row) ?? 0) == 0)
                    id.SetValue(row, rows.Count(x => x.GetType() == row.GetType() && (int)(x.GetType().GetProperty("Id")?.GetValue(x) ?? 0) > 0) + 1);
            }
            foreach (var tenant in Rows<Tenant>())
            {
                foreach (var member in tenant.Members) member.TenantId = tenant.Id;
                foreach (var subscription in tenant.ProductSubscriptions) subscription.TenantId = tenant.Id;
            }
            return Task.CompletedTask;
        }
    }
}
