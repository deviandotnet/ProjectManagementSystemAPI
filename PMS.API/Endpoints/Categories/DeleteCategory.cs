using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.Categories.DeleteCategory;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.Categories;

internal sealed class DeleteCategory : IApiEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapDelete("api/projects/{projectId:guid}/categories/{id:guid}", async (
            Guid projectId,
            Guid id,
            ICommandHandler<DeleteCategoryCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new DeleteCategoryCommand(projectId, id);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.NoContent(),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("Delete Category")
        .WithDescription("Deletes a category. Project owner/admins can delete any category; members can delete their own created categories.")
        .WithTags(Tags.Categories);
    }
}
