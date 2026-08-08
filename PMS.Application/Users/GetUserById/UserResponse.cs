using PMS.Domain.Users;

namespace PMS.Application.Users.GetUserById;

/// <summary>
/// Display-only user details DTO (excludes audit and authentication properties).
/// </summary>
public sealed record UserResponse(
    Guid Id,
    string FirstName,
    string? MiddleName,
    string LastName,
    string Email,
    SystemRole SystemRole,
    bool IsActive
);
