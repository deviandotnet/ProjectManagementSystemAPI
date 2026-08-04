using PMS.Application.Abstractions.Messaging;

namespace PMS.Application.Users.RegisterUser;

public sealed record RegisterUserCommand(
    string FirstName,
    string? MiddleName,
    string LastName,
    string Email,
    string Password) : ICommand<Guid>;
