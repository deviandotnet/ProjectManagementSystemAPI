using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.ProjectMembers.RemoveProjectMember;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.ProjectMembers;

internal sealed class RemoveProjectMember : IApiEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/projects/{id:guid}/members/{userId:guid}", async (
            Guid id,
            Guid userId,
            ICommandHandler<RemoveProjectMemberCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RemoveProjectMemberCommand(id, userId);
            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.NoContent(),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("Remove Project Member")
        .WithDescription("Removes a member from the specified project by project ID and user ID. Requires ProjectManager authorization.")
        .WithTags(Tags.Projects);
    }
}
