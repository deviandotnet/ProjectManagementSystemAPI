using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.SubCategories.GetSubCategoriesByCategoryId;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.SubCategories;

internal sealed class GetSubCategoriesByCategoryId : IApiEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/categories/{categoryId:guid}/subcategories", async (
            Guid categoryId,
            IQueryHandler<GetSubCategoriesByCategoryIdQuery, IReadOnlyCollection<SubCategoryResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetSubCategoriesByCategoryIdQuery(categoryId);
            Result<IReadOnlyCollection<SubCategoryResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(
                subCategories => Results.Ok(subCategories),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("List SubCategories")
        .WithDescription("Retrieves all subcategories for a given category ID ordered by display order.")
        .WithTags(Tags.Categories);
    }
}
