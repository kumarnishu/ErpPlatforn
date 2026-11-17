using Microsoft.AspNetCore.Identity;
using Platform.Application.Abstractions;
using Platform.Domain;

namespace Platform.Infrastructure.Security;

public sealed class PasswordHashService : IPasswordHashService
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(string password)
    {
        var user = new User();
        return _hasher.HashPassword(user, password);
    }
}
