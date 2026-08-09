using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.ActionItems.GetActionItems;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.ActionItems;

internal sealed class GetActionItems : IApiEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/projects/{projectId:guid}/action-items", async (
            Guid projectId,
            Guid? categoryId,
            Guid? subCategoryId,
            string? status,
            int? priority,
            string? ownerName,
            string? search,
            string? weekStart,
            string? weekEnd,
            DateOnly? startDate,
            DateOnly? endDate,
            IQueryHandler<GetActionItemsQuery, IReadOnlyCollection<ActionItemResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            int[]? statuses = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                statuses = status.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse)
                    .ToArray();
            }

            var query = new GetActionItemsQuery(
                projectId,
                categoryId,
                subCategoryId,
                statuses,
                priority,
                ownerName,
                search,
                weekStart,
                weekEnd,
                startDate,
                endDate);

            Result<IReadOnlyCollection<ActionItemResponse>> result =
                await handler.Handle(query, cancellationToken);

            return result.Match(
                items => Results.Ok(items),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("List Action Items")
        .WithDescription("Retrieves all action items for a project with computed status. Supports filtering by category, subcategory, status, priority, owner, search text, week range, and date range.")
        .WithTags(Tags.ActionItems);
    }
}
