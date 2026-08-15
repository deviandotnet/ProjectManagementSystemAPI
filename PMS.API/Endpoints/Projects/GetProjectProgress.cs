using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.Projects.GetProjectProgress;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.Projects;

internal sealed class GetProjectProgress : IApiEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/projects/{projectId:guid}/progress", async (
            Guid projectId,
            IQueryHandler<GetProjectProgressQuery, ProjectProgressResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetProjectProgressQuery(projectId);

            Result<ProjectProgressResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(
                progress => Results.Ok(progress),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("Get Project Progress")
        .WithDescription("Calculates and returns real-time progress KPI metrics (Count-based or Weight-based), completed counts, and action item status distributions for a specific project.")
        .WithTags(Tags.Projects);
    }
}
