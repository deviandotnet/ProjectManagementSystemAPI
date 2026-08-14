using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.Projects.GetTimeline;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.Projects;

internal sealed class GetTimeline : IApiEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/projects/{projectId:guid}/timeline", async (
            Guid projectId,
            TimelineScale? scale,
            DateOnly? startDate,
            DateOnly? endDate,
            IQueryHandler<GetTimelineQuery, TimelineResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetTimelineQuery(projectId, scale, startDate, endDate);

            Result<TimelineResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(
                response => Results.Ok(response),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("Get Project Timeline")
        .WithDescription("Calculates and returns structured grid timeline data (date columns, row hierarchy, week column indexes, and status labels) for CSS Grid rendering.")
        .WithTags(Tags.Projects);
    }
}
