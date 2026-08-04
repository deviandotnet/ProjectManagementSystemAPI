using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.Projects.UpdateProject;
using PMS.Domain.Projects;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.Projects;

internal sealed class UpdateProject : IApiEndpoint
{
    public sealed record UpdateProjectRequest(
        string Name,
        string? Description,
        DateOnly StartDate,
        DateOnly EndDate,
        int WeekStartDay = 1,
        TimelineScale DefaultTimelineScale = TimelineScale.Weekly,
        ProgressMode ProgressMode = ProgressMode.CountBased,
        ProjectStatus Status = ProjectStatus.Active
    );

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/projects/{id:guid}", async (
            Guid id,
            UpdateProjectRequest request,
            ICommandHandler<UpdateProjectCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateProjectCommand(
                id,
                request.Name,
                request.Description,
                request.StartDate,
                request.EndDate,
                request.WeekStartDay,
                request.DefaultTimelineScale,
                request.ProgressMode,
                request.Status);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.NoContent(),
                CustomResults.Problem);
        })
        .RequireAuthorization("RequireProjectManager")
        .WithSummary("Update Project")
        .WithDescription("Updates project settings, timeline scale, progress mode, and status by project ID. Requires ProjectManager role or higher.")
        .WithTags(Tags.Projects);
    }
}
