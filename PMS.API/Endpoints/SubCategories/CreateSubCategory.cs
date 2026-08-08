using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.SubCategories.CreateSubCategory;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.SubCategories;

internal sealed class CreateSubCategory : IApiEndpoint
{
    public sealed record CreateSubCategoryRequest(
        string Name,
        int DisplayOrder = 0
    );

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/categories/{categoryId:guid}/subcategories", async (
            Guid categoryId,
            CreateSubCategoryRequest request,
            ICommandHandler<CreateSubCategoryCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateSubCategoryCommand(categoryId, request.Name, request.DisplayOrder);
            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                id => Results.Created($"/api/categories/{categoryId}/subcategories/{id}", id),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("Create SubCategory")
        .WithDescription("Creates a new subcategory under the specified category ID.")
        .WithTags(Tags.Categories);
    }
}
