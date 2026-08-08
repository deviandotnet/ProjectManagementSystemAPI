using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.SubCategories.DeleteSubCategory;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.SubCategories;

internal sealed class DeleteSubCategory : IApiEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/categories/{categoryId:guid}/subcategories/{id:guid}", async (
            Guid categoryId,
            Guid id,
            ICommandHandler<DeleteSubCategoryCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteSubCategoryCommand(categoryId, id);
            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.NoContent(),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("Delete SubCategory")
        .WithDescription("Permanently deletes a subcategory by category ID and subcategory ID.")
        .WithTags(Tags.Categories);
    }
}
