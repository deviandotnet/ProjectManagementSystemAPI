using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.Users.GetUserById;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.Users;

internal sealed class GetUserById : IApiEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/users/{id:guid}", async (
            Guid id,
            IQueryHandler<GetUserByIdQuery, UserResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetUserByIdQuery(id);

            Result<UserResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(
                user => Results.Ok(user),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("Get User by ID")
        .WithDescription("Retrieves display-only details for a specific registered user by user ID.")
        .WithTags(Tags.Users);
    }
}
