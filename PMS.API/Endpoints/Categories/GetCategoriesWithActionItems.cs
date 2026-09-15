using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.Categories.GetCategoriesWithActionItems;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.Categories;

internal sealed class GetCategoriesWithActionItems : IApiEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/projects/{projectId:guid}/categories/action-items", async (
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
            int? pageNumber,
            int? pageSize,
            IQueryHandler<GetCategoriesWithActionItemsQuery, CategoriesWithActionItemsResponse> handler,
            CancellationToken cancellationToken) =>
        {
            int[]? statuses = null;

            if (!string.IsNullOrWhiteSpace(status))
            {
                statuses = status
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(int.Parse)
                    .ToArray();
            }

            var query = new GetCategoriesWithActionItemsQuery(
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
                endDate,
                pageNumber ?? 1,
                pageSize ?? 20);

            Result<CategoriesWithActionItemsResponse> result =
                await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("List Categories With Action Items")
        .WithDescription("Retrieves every applicable project category with its matching page of action items. Empty categories contain an empty actionItems collection. Pagination counts action items, and all action-item filters are supported.")
        .WithTags(Tags.Categories, Tags.ActionItems);
    }
}
