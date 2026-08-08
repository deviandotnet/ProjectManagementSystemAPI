using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.SubCategories.UpdateSubCategory;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.SubCategories;

internal sealed class UpdateSubCategory : IApiEndpoint
{
    public sealed record UpdateSubCategoryRequest(
        string Name,
        int DisplayOrder
    );

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/categories/{categoryId:guid}/subcategories/{id:guid}", async (
            Guid categoryId,
            Guid id,
            UpdateSubCategoryRequest request,
            ICommandHandler<UpdateSubCategoryCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateSubCategoryCommand(categoryId, id, request.Name, request.DisplayOrder);
            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.NoContent(),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("Update SubCategory")
        .WithDescription("Updates a subcategory's name and display order by category ID and subcategory ID.")
        .WithTags(Tags.Categories);
    }
}
