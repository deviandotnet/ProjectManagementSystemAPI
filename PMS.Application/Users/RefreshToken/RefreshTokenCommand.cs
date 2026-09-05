using PMS.Application.Abstractions.Messaging;

namespace PMS.Application.Users.RefreshToken;

public sealed record RefreshTokenCommand(string Token) : ICommand<AccessTokenResponse>;
