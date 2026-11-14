using BuildingBlocks.Http;
using Platform.Application.Abstractions;
using Platform.Application.Contracts;
using Platform.Application.Services;
using Platform.Domain;
using Xunit;

namespace Platform.Application.Tests;

public sealed class TenantSignupServiceTests
{
    // A successful signup creates the tenant, platform account, and their admin membership as one linked set.
    [Fact]
    public async Task SignUpAsync_CreatesTenantAndAdminMembership()
    {
        var repository = new FakeRepository();
        var service = new TenantSignupService(repository, new FakePasswordHasher());

        var result = await service.SignUpAsync(
            new TenantSignupRequest(" Acme Inc ", " ACME ", " Ada Lovelace ", " Ada@Example.com ", "long-password-123"),
            CancellationToken.None);

        Assert.Equal("acme", result.TenantKey);
        Assert.Equal("Acme Inc", result.TenantName);
        Assert.Equal("Ada@Example.com", result.AdminEmail);
        Assert.Equal("Ada Lovelace", result.AdminName);
        Assert.Equal(nameof(TenantMembershipRole.TenantAdmin), result.Role);
        Assert.Equal("hashed:long-password-123", repository.User!.PasswordHash);
        Assert.Equal(result.AdminUserId, repository.Membership!.UserId);
        Assert.Same(repository.Tenant, repository.Membership.Tenant);
        Assert.Same(repository.User, repository.Membership.User);
        Assert.Equal(TenantMembershipRole.TenantAdmin, repository.Membership.Role);
        Assert.Contains(repository.Membership, repository.Tenant.Memberships);
        Assert.Contains(repository.Membership, repository.User.Memberships);
        Assert.Equal("acme", repository.LastCheckedTenantKey);
        Assert.Equal("ADA@EXAMPLE.COM", repository.LastCheckedEmail);
    }

    // Existing records should return a client conflict before any signup write is attempted.
    [Fact]
    public async Task SignUpAsync_RejectsExistingTenantKey()
    {
        var repository = new FakeRepository { TenantKeyExists = true };
        var service = new TenantSignupService(repository, new FakePasswordHasher());

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.SignUpAsync(ValidRequest(), CancellationToken.None));

        Assert.Equal(409, exception.StatusCode);
        Assert.Equal("TENANT_KEY_EXISTS", exception.Code);
        Assert.Equal(0, repository.AddCount);
    }

    [Fact]
    public async Task SignUpAsync_RejectsExistingEmail()
    {
        var repository = new FakeRepository { EmailExists = true };
        var service = new TenantSignupService(repository, new FakePasswordHasher());

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.SignUpAsync(ValidRequest(), CancellationToken.None));

        Assert.Equal(409, exception.StatusCode);
        Assert.Equal("EMAIL_EXISTS", exception.Code);
        Assert.Equal("NEW@EXAMPLE.COM", repository.LastCheckedEmail);
        Assert.Equal(0, repository.AddCount);
    }

    // Invalid requests must be rejected before the repository is queried or changed.
    [Theory]
    [InlineData("", "tenant", "Admin", "admin@example.com", "valid-password-123", "INVALID_TENANT_NAME")]
    [InlineData("Tenant", "", "Admin", "admin@example.com", "valid-password-123", "INVALID_TENANT_KEY")]
    [InlineData("Tenant", "tenant", "", "admin@example.com", "valid-password-123", "INVALID_ADMIN_NAME")]
    [InlineData("Tenant", "tenant", "Admin", "not-an-email", "valid-password-123", "INVALID_EMAIL")]
    [InlineData("Tenant", "tenant", "Admin", "admin@example.com", "short", "INVALID_PASSWORD")]
    public async Task SignUpAsync_RejectsInvalidInput(string name, string key, string admin, string email, string password, string expectedCode)
    {
        var repository = new FakeRepository();
        var service = new TenantSignupService(repository, new FakePasswordHasher());

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.SignUpAsync(
            new TenantSignupRequest(name, key, admin, email, password), CancellationToken.None));

        Assert.Equal(400, exception.StatusCode);
        Assert.Equal(expectedCode, exception.Code);
        Assert.Equal(0, repository.AddCount);
        Assert.Null(repository.LastCheckedTenantKey);
    }

    // Unique database constraints can catch races after the initial existence checks; expose them as API conflicts.
    [Fact]
    public async Task SignUpAsync_MapsPersistenceTenantKeyConflict()
    {
        var repository = new FakeRepository { Conflict = "tenant-key" };
        var service = new TenantSignupService(repository, new FakePasswordHasher());

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.SignUpAsync(ValidRequest(), CancellationToken.None));

        Assert.Equal(409, exception.StatusCode);
        Assert.Equal("TENANT_KEY_EXISTS", exception.Code);
    }

    [Fact]
    public async Task SignUpAsync_MapsPersistenceEmailConflict()
    {
        var repository = new FakeRepository { Conflict = "email" };
        var service = new TenantSignupService(repository, new FakePasswordHasher());

        var exception = await Assert.ThrowsAsync<ApiException>(() => service.SignUpAsync(ValidRequest(), CancellationToken.None));

        Assert.Equal(409, exception.StatusCode);
        Assert.Equal("EMAIL_EXISTS", exception.Code);
    }

    private static TenantSignupRequest ValidRequest() =>
        new("Example Tenant", "example", "New Admin", "new@example.com", "valid-password-123");

    // Keep tests independent of real password hashing while still checking that the service passes the password through.
    private sealed class FakePasswordHasher : IPasswordHashService
    {
        public string Hash(string password) => $"hashed:{password}";
    }

    // In-memory fake captures calls and created entities so service behavior can be tested without a database.
    private sealed class FakeRepository : ITenantSignupRepository
    {
        public bool TenantKeyExists { get; init; }
        public bool EmailExists { get; init; }
        public string? Conflict { get; init; }
        public string? LastCheckedTenantKey { get; private set; }
        public string? LastCheckedEmail { get; private set; }
        public int AddCount { get; private set; }
        public Tenant? Tenant { get; private set; }
        public PlatformUser? User { get; private set; }
        public TenantMembership? Membership { get; private set; }

        public Task<bool> TenantKeyExistsAsync(string key, CancellationToken cancellationToken)
        {
            LastCheckedTenantKey = key;
            return Task.FromResult(TenantKeyExists);
        }

        public Task<bool> EmailExistsAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            LastCheckedEmail = normalizedEmail;
            return Task.FromResult(EmailExists);
        }

        public Task AddTenantWithAdminAsync(Tenant tenant, PlatformUser user, TenantMembership membership, CancellationToken cancellationToken)
        {
            AddCount++;
            if (Conflict is not null)
                throw new TenantSignupConflictException(Conflict);

            Tenant = tenant;
            User = user;
            Membership = membership;
            tenant.Id = 42;
            return Task.CompletedTask;
        }
    }
}
