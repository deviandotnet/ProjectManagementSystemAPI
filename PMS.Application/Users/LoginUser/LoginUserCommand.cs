using PMS.Application.Abstractions.Messaging;
using PMS.Application.Users;

namespace PMS.Application.Users.LoginUser
{
    public sealed record LoginUserCommand(
        string Email,
        string Password) : ICommand<AccessTokenResponse>;
}
