using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.Users;
using PMS.Application.Users.LoginUser;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.Users
{
    internal sealed class LoginUser : IApiEndpoint
    {
        public sealed record LoginUserRequest(string Email, string Password);

        public void MapEndpoint(IEndpointRouteBuilder app)
        {
            app.MapPost("api/users/login", async (
                LoginUserRequest request,
                ICommandHandler<LoginUserCommand, AccessTokenResponse> handler,
                CancellationToken cancellationToken) =>
            {
                var command = new LoginUserCommand(request.Email, request.Password);

                Result<AccessTokenResponse> result = await handler.Handle(command, cancellationToken);

                return result.Match(Results.Ok, CustomResults.Problem);
            })
            .WithSummary("Login User")
            .WithDescription("Authenticates a registered user with email and password, returning a JWT bearer access token for authorized requests.")
            .WithTags(Tags.Users);
        }
    }
}
