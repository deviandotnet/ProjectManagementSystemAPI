using PMS.Application.Abstractions.Messaging;

namespace PMS.Application.Users.CreateUser;

public sealed record CreateUserCommand(
    string FirstName,
    string? MiddleName,
    string LastName,
    string Email,
    string Password) : ICommand<Guid>;
