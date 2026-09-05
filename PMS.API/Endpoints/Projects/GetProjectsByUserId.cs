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
            int? pageNumber,
            int? pageSize,
            IQueryHandler<GetProjectsByUserIdQuery, PagedResponse<ProjectResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            if (!userContext.UserId.HasValue)
            {
                return CustomResults.Problem(UserErrors.Unauthorized);
            }

            var query = new GetProjectsByUserIdQuery(
                userContext.UserId.Value,
                pageNumber ?? 1,
                pageSize ?? 20);

            Result<PagedResponse<ProjectResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(
                projects => Results.Ok(projects),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("List Projects for Current User")
        .WithDescription("Retrieves a paginated list of projects for the currently authenticated user.")
        .WithTags(Tags.Projects);

        app.MapGet("api/users/{userId:guid}/projects", async (
            Guid userId,
            int? pageNumber,
            int? pageSize,
            IQueryHandler<GetProjectsByUserIdQuery, PagedResponse<ProjectResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetProjectsByUserIdQuery(
                userId,
                pageNumber ?? 1,
                pageSize ?? 20);

            Result<PagedResponse<ProjectResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(
                projects => Results.Ok(projects),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("Get Projects by User ID")
        .WithDescription("Retrieves a paginated list of projects created by or associated with a specific user ID.")
        .WithTags(Tags.Projects);
    }
}
