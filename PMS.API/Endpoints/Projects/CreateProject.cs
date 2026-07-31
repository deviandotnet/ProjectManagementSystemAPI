using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.Projects.CreateProject;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.Projects;

internal sealed class CreateProject : IApiEndpoint
{
    public sealed record ProjectRequest(
        string Name,
        string? Description,
        DateOnly StartDate,
        DateOnly EndDate,
        int WeekStartDay = 1,
        TimelineScale DefaultTimelineScale = TimelineScale.Weekly,
        ProgressMode ProgressMode = ProgressMode.CountBased,
        Guid? CreatedByUserId = null
    );

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/projects", async (
            ProjectRequest request,
            ICommandHandler<CreateProjectCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateProjectCommand(
                request.Name,
                request.Description,
                request.StartDate,
                request.EndDate,
                request.WeekStartDay,
                request.DefaultTimelineScale,
                request.ProgressMode,
                request.CreatedByUserId);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                id => Results.Created($"/api/projects/{id}", id),
                CustomResults.Problem);
        })
        .WithTags(Tags.Projects);
    }
}
