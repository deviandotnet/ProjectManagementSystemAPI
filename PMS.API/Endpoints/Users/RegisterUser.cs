using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.Users.RegisterUser;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.Users;

internal sealed class RegisterUser : IApiEndpoint
{
    public sealed record RegisterUserRequest(
        string FirstName,
        string? MiddleName,
        string LastName,
        string Email,
        string Password
    );

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/users", async (
            RegisterUserRequest request,
            ICommandHandler<RegisterUserCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RegisterUserCommand(
                request.FirstName,
                request.MiddleName,
                request.LastName,
                request.Email,
                request.Password);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                id => Results.Created($"/api/users/{id}", id),
                CustomResults.Problem);
        })
        .WithSummary("Register User")
        .WithDescription("Registers a new user account with first name, last name, unique email, and password.")
        .WithTags(Tags.Users);
    }
}
