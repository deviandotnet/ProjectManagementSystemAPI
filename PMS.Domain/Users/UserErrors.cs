using PMS.SharedKernel;

namespace PMS.Domain.Users;

public static class UserErrors
{
    public static Error NotFound(Guid userId) => Error.NotFound(
        "Users.NotFound",
        $"The user with the Id = '{userId}' was not found.");

    public static Error EmailAlreadyExists(string email) => Error.Conflict(
        "Users.EmailAlreadyExists",
        $"The email '{email}' is already registered.");

    public static Error Unauthorized() => Error.Failure(
        "Users.Unauthorized",
        "User is unauthorized.");
}
