using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.Categories.GetCategoriesByProjectId;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.Categories;

internal sealed class GetCategoriesByProjectId : IApiEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/projects/{projectId:guid}/categories", async (
            Guid projectId,
            int? pageNumber,
            int? pageSize,
            IQueryHandler<GetCategoriesByProjectIdQuery, PagedResponse<CategoryResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetCategoriesByProjectIdQuery(
                projectId,
                pageNumber ?? 1,
                pageSize ?? 20);

            Result<PagedResponse<CategoryResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(
                categories => Results.Ok(categories),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("Get Categories by Project ID")
        .WithDescription("Retrieves a paginated list of categories within a project ordered by display order.")
        .WithTags(Tags.Categories);
    }
}
