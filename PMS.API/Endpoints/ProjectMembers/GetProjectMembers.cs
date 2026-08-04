using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.ProjectMembers.GetProjectMembers;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.ProjectMembers;

internal sealed class GetProjectMembers : IApiEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/projects/{id:guid}/members", async (
            Guid id,
            IQueryHandler<GetProjectMembersQuery, List<ProjectMemberResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetProjectMembersQuery(id);
            Result<List<ProjectMemberResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(
                members => Results.Ok(members),
                CustomResults.Problem);
        })
        .RequireAuthorization("RequireProjectMember")
        .WithSummary("Get Project Members")
        .WithDescription("Retrieves all members assigned to the specified project along with their names, emails, project-level roles, and join dates. Requires Project Member authorization.")
        .WithTags(Tags.Projects);
    }
}
