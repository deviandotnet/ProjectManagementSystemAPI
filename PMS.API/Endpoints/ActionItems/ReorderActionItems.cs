using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.ActionItems.ReorderActionItems;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.ActionItems;

internal sealed class ReorderActionItems : IApiEndpoint
{
    public sealed record ReorderActionItemsRequest(
        List<ReorderActionItemItem> Items
    );

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/projects/{projectId:guid}/action-items/reorder", async (
            Guid projectId,
            ReorderActionItemsRequest request,
            ICommandHandler<ReorderActionItemsCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new ReorderActionItemsCommand(projectId, request.Items ?? []);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.NoContent(),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("Reorder Action Items")
        .WithDescription("Reorders action items within a project by updating their sequence values.")
        .WithTags(Tags.ActionItems);
    }
}
