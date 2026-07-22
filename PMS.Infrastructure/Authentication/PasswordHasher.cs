using PMS.Application.Abstractions.Authentication;

namespace PMS.Infrastructure.Authentication;

/// <summary>
/// BCrypt implementation of IPasswordHasher using BCrypt.Net-Next.
/// Provides salted, work-factor tuned password hashing and constant-time verification.
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    public bool Verify(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }
}
