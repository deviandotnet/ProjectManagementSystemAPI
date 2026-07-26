namespace PMS.Application.Features.UserFeatures.GetUserById;

/// <summary>
/// Request DTO for retrieving a user by ID.
/// </summary>
public sealed record GetUserByIdRequest(Guid UserId);

/// <summary>
/// Response DTO for GetUserById.
/// Excludes sensitive information like PasswordHash.
/// </summary>
public sealed record GetUserByIdResponse(
    Guid Id,
    string FirstName,
    string? MiddleName,
    string LastName,
    string Email,
    bool IsActive,
    Guid? CreatedByUserId,
    DateTimeOffset CreatedAt
);
