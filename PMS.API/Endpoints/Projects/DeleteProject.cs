using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.Projects.DeleteProject;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.Projects;

internal sealed class DeleteProject : IApiEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/projects/{id:guid}", async (
            Guid id,
            ICommandHandler<DeleteProjectCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteProjectCommand(id);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.NoContent(),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("Delete Project")
        .WithDescription("Permanently deletes a project and all associated data by project ID. Requires Project Manager authorization.")
        .WithTags(Tags.Projects);
    }
}
