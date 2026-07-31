using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.Users.CreateUser;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.Users;

internal sealed class CreateUser : IApiEndpoint
{
    public sealed record UserRequest(
        string FirstName,
        string? MiddleName,
        string LastName,
        string Email,
        string Password
    );

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/users", async (
            UserRequest request,
            ICommandHandler<CreateUserCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateUserCommand(
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
        .WithTags(Tags.Users);
    }
}
