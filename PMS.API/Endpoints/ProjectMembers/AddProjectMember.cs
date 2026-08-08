using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.ProjectMembers.AddProjectMember;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.ProjectMembers;

internal sealed class AddProjectMember : IApiEndpoint
{
    public sealed record AddMemberRequest(Guid UserId, UserRole Role);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/projects/{id:guid}/members", async (
            Guid id,
            AddMemberRequest request,
            ICommandHandler<AddProjectMemberCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new AddProjectMemberCommand(id, request.UserId, request.Role);
            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.Created($"/api/projects/{id}/members", null),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("Add Project Member")
        .WithDescription("Assigns a registered user to the specified project with a project-level role (`Admin`, `ProjectManager`, `TeamLeader`, `Member`, `Viewer`). Requires ProjectManager authorization.")
        .WithTags(Tags.Projects);
    }
}
