using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.ActionItems.DeleteActionItem;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.ActionItems;

internal sealed class DeleteActionItem : IApiEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/projects/{projectId:guid}/action-items/{id:guid}", async (
            Guid projectId,
            Guid id,
            ICommandHandler<DeleteActionItemCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteActionItemCommand(projectId, id);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.NoContent(),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("Delete Action Item")
        .WithDescription("Permanently deletes an action item and its associated schedule/execution metrics. Requires TeamLead role or higher.")
        .WithTags(Tags.ActionItems);
    }
}
