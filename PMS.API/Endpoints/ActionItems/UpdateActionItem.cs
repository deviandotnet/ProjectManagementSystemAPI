using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.ActionItems.UpdateActionItem;
using PMS.Domain.ActionItems;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.ActionItems;

internal sealed class UpdateActionItem : IApiEndpoint
{
    public sealed record UpdateActionItemRequest(
        Guid CategoryId,
        Guid? SubCategoryId,
        string ActionItemName,
        string? Description,
        int Priority,
        string? OwnerName,
        Guid? OwnerId,
        decimal? Weight,
        int Sequence,
        string? Remarks,
        DateOnly PlannedStartDate,
        DateOnly PlannedEndDate,
        DateOnly? ActualStartDate = null,
        DateOnly? ActualEndDate = null,
        decimal? ActualHours = null,
        string? DelayReason = null
    );

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/projects/{projectId:guid}/action-items/{id:guid}", async (
            Guid projectId,
            Guid id,
            UpdateActionItemRequest request,
            ICommandHandler<UpdateActionItemCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateActionItemCommand(
                projectId,
                id,
                request.CategoryId,
                request.SubCategoryId,
                request.ActionItemName,
                request.Description,
                (Priority)request.Priority,
                request.OwnerName,
                request.OwnerId,
                request.Weight,
                request.Sequence,
                request.Remarks,
                request.PlannedStartDate,
                request.PlannedEndDate,
                request.ActualStartDate,
                request.ActualEndDate,
                request.ActualHours,
                request.DelayReason);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.NoContent(),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("Update Action Item")
        .WithDescription("Updates an existing action item details, schedule, and actual execution metrics.")
        .WithTags(Tags.ActionItems);
    }
}
