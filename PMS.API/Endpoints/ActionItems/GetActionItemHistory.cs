using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.ActionItems.GetActionItemHistory;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.ActionItems;

internal sealed class GetActionItemHistory : IApiEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/projects/{projectId:guid}/action-items/{id:guid}/history", async (
            Guid projectId,
            Guid id,
            IQueryHandler<GetActionItemHistoryQuery, IReadOnlyCollection<ActionItemHistoryResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetActionItemHistoryQuery(projectId, id);

            Result<IReadOnlyCollection<ActionItemHistoryResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(
                history => Results.Ok(history),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("Get Action Item Audit History")
        .WithDescription("Retrieves the full audit log history of changes performed on a specific action item.")
        .WithTags(Tags.ActionItems);
    }
}
