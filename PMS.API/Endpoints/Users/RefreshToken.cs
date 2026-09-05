using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.Users;
using PMS.Application.Users.RefreshToken;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.Users;

internal sealed class RefreshToken : IApiEndpoint
{
    public sealed record RefreshTokenRequest(string RefreshToken);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/auth/refresh", async (
            RefreshTokenRequest request,
            ICommandHandler<RefreshTokenCommand, AccessTokenResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RefreshTokenCommand(request.RefreshToken);

            Result<AccessTokenResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithSummary("Refresh Access Token")
        .WithDescription("Rotates a valid refresh token and returns a new access and refresh token pair.")
        .WithTags(Tags.Users)
        .AllowAnonymous();
    }
}
