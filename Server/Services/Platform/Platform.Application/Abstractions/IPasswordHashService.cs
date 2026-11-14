namespace Platform.Application.Abstractions;

public interface IPasswordHashService
{
    string Hash(string password);
}
