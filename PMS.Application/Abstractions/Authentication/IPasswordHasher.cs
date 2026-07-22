namespace PMS.Application.Abstractions.Authentication;

/// <summary>
/// Abstraction for password hashing and verification.
/// Prevents the Application layer from depending directly on third-party security libraries.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string passwordHash);
}
