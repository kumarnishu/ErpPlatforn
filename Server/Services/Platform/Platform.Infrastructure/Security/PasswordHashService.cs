using Microsoft.AspNetCore.Identity;
using Platform.Application.Abstractions;
using Platform.Domain;

namespace Platform.Infrastructure.Security;

public sealed class PasswordHashService : IPasswordHashService
{
    private readonly PasswordHasher<PlatformUser> _hasher = new();

    public string Hash(string password)
    {
        var user = new PlatformUser();
        return _hasher.HashPassword(user, password);
    }
}
