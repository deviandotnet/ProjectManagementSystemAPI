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
            IQueryHandler<GetCategoriesByProjectIdQuery, IReadOnlyCollection<CategoryResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetCategoriesByProjectIdQuery(projectId);

            Result<IReadOnlyCollection<CategoryResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(
                categories => Results.Ok(categories),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("Get Categories by Project ID")
        .WithDescription("Retrieves all categories within a project ordered by display order.")
        .WithTags(Tags.Categories);
    }
}
