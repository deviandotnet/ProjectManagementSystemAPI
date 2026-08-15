using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.Projects.GetProjectAuditFeed;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.Projects;

internal sealed class GetProjectAuditFeed : IApiEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/projects/{projectId:guid}/audit", async (
            Guid projectId,
            IQueryHandler<GetProjectAuditFeedQuery, AuditFeedResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetProjectAuditFeedQuery(projectId);

            Result<AuditFeedResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(
                feed => Results.Ok(feed),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("Get Project Audit Feed")
        .WithDescription("Retrieves the chronological audit log and human-readable activity feed for a project and all its child categories, subcategories, action items, schedules, and members. Requires TeamLead role or higher.")
        .WithTags(Tags.Audit);
    }
}
