using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.Projects.GetProjectsByUserId;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.Projects;

internal sealed class GetProjectsByUserId : IApiEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/projects", async (
            IUserContext userContext,
            IQueryHandler<GetProjectsByUserIdQuery, List<ProjectResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            if (!userContext.UserId.HasValue)
            {
                return CustomResults.Problem(UserErrors.Unauthorized);
            }

            var query = new GetProjectsByUserIdQuery(userContext.UserId.Value);

            Result<List<ProjectResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(
                projects => Results.Ok(projects),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("List Projects for Current User")
        .WithDescription("Retrieves all projects for the currently authenticated user.")
        .WithTags(Tags.Projects);

        app.MapGet("api/users/{userId:guid}/projects", async (
            Guid userId,
            IQueryHandler<GetProjectsByUserIdQuery, List<ProjectResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetProjectsByUserIdQuery(userId);

            Result<List<ProjectResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(
                projects => Results.Ok(projects),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("Get Projects by User ID")
        .WithDescription("Retrieves all projects created by or associated with a specific user ID.")
        .WithTags(Tags.Projects);
    }
}
