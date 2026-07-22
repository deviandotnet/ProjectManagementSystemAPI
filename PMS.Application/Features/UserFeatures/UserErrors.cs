using PMS.Domain.Abstractions.Errors;

namespace PMS.Application.Features.UserFeatures;

/// <summary>
/// Centralised error definitions for the User feature slice.
/// Every conditional error returned by any User handler is defined here
/// to ensure consistent error codes and descriptions across all User endpoints.
/// 
/// Naming convention: User.{Operation} — e.g. "User.NotFound"
/// </summary>
public static class UserErrors
{
    public static readonly Error InvalidId =
        Error.Validation("User.InvalidId", "The provided User ID is not a valid GUID format.");

    public static Error NotFound(Guid userId) =>
        Error.NotFound("User.NotFound", $"User with ID '{userId}' was not found.");

    public static Error EmailAlreadyExists(string email) =>
        Error.Conflict("User.EmailAlreadyExists", $"A user with the email '{email}' already exists.");

    public static readonly Error NoUsersFound =
        Error.NotFound("User.NoUsersFound", "No users were found.");
}
