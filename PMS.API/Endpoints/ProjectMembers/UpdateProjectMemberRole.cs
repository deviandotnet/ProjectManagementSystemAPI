using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.ProjectMembers.UpdateProjectMemberRole;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.ProjectMembers;

internal sealed class UpdateProjectMemberRole : IApiEndpoint
{
    public sealed record UpdateMemberRoleRequest(UserRole Role);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/projects/{id:guid}/members/{userId:guid}", async (
            Guid id,
            Guid userId,
            UpdateMemberRoleRequest request,
            ICommandHandler<UpdateProjectMemberRoleCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateProjectMemberRoleCommand(id, userId, request.Role);
            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.NoContent(),
                CustomResults.Problem);
        })
        .RequireAuthorization("RequireProjectManager")
        .WithSummary("Update Project Member Role")
        .WithDescription("Updates an existing project member's role (`Admin`, `ProjectManager`, `TeamLeader`, `Member`, `Viewer`) by project ID and user ID. Requires ProjectManager authorization.")
        .WithTags(Tags.Projects);
    }
}
