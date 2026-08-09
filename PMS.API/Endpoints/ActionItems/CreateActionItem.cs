using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.ActionItems.CreateActionItem;
using PMS.Domain.ActionItems;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.ActionItems;

internal sealed class CreateActionItem : IApiEndpoint
{
    public sealed record CreateActionItemRequest(
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
        app.MapPost("api/projects/{projectId:guid}/action-items", async (
            Guid projectId,
            CreateActionItemRequest request,
            ICommandHandler<CreateActionItemCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateActionItemCommand(
                projectId,
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

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                id => Results.Created($"/api/projects/{projectId}/action-items/{id}", id),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("Create Action Item")
        .WithDescription("Creates a new action item under the specified project ID along with its planned schedule and optional actual execution.")
        .WithTags(Tags.ActionItems);
    }
}
