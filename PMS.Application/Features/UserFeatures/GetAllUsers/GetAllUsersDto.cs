namespace PMS.Application.Features.UserFeatures.GetAllUsers;

/// <summary>
/// Request for retrieving all users.
/// </summary>
public sealed record GetAllUsersRequest;

/// <summary>
/// Response DTO for GetAllUsers.
/// Excludes sensitive information like PasswordHash.
/// </summary>
public sealed record GetAllUsersResponse(
    Guid Id,
    string FirstName,
    string? MiddleName,
    string LastName,
    string Email,
    bool IsActive,
    Guid? CreatedByUserId,
    DateTimeOffset CreatedAt
);
