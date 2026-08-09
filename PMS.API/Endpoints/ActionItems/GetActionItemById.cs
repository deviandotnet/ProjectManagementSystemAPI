using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.ActionItems.GetActionItemById;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.ActionItems;

internal sealed class GetActionItemById : IApiEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/projects/{projectId:guid}/action-items/{id:guid}", async (
            Guid projectId,
            Guid id,
            IQueryHandler<GetActionItemByIdQuery, ActionItemResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetActionItemByIdQuery(projectId, id);

            Result<ActionItemResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(
                item => Results.Ok(item),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("Get Action Item by ID")
        .WithDescription("Retrieves a single action item within a project by its ID, including schedule, execution details, and computed status.")
        .WithTags(Tags.ActionItems);
    }
}
