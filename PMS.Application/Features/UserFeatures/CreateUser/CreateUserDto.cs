namespace PMS.Application.Features.UserFeatures.CreateUser;

/// <summary>
/// Request DTO for creating a new user account.
/// </summary>
public sealed record CreateUserRequest(
    string FirstName,
    string? MiddleName,
    string LastName,
    string Email,
    string Password,
    Guid? CreatedByUserId = null
);

/// <summary>
/// Response DTO for CreateUser.
/// Excludes sensitive information like PasswordHash.
/// </summary>
public sealed record CreateUserResponse(
    Guid Id,
    string FirstName,
    string? MiddleName,
    string LastName,
    string Email,
    bool IsActive,
    Guid? CreatedByUserId,
    DateTimeOffset CreatedAt
);
