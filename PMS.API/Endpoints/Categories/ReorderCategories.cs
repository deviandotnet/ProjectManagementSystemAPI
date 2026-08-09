using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.Categories.ReorderCategories;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.Categories;

internal sealed class ReorderCategories : IApiEndpoint
{
    public sealed record ReorderCategoriesRequest(
        List<ReorderCategoryItem> Items
    );

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/projects/{projectId:guid}/categories/reorder", async (
            Guid projectId,
            ReorderCategoriesRequest request,
            ICommandHandler<ReorderCategoriesCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new ReorderCategoriesCommand(projectId, request.Items ?? []);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.NoContent(),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("Reorder Categories")
        .WithDescription("Reorders categories within a project by updating their display order sequence.")
        .WithTags(Tags.Categories);
    }
}
