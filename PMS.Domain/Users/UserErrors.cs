using PMS.SharedKernel;

namespace PMS.Domain.Users;

public static class UserErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Users.NotFound",
        $"The user was not found.");

    public static Error NotFoundById = Error.NotFound(
        "Users.NotFound",
        $"The user with specified Id was not found.");

    public static readonly Error EmailAlreadyExists = Error.Conflict(
        "Users.EmailAlreadyExists",
        $"The email is already registered.");

    public static readonly Error Unauthorized = Error.Failure(
        "Users.Unauthorized",
        "User is unauthorized.");

    public static readonly Error NotFoundByEmail = Error.NotFound(
        "Users.NotFoundByEmail",
        "The user with the specified email was not found.");
}
