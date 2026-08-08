using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.Categories.UpdateCategory;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.Categories;

internal sealed class UpdateCategory : IApiEndpoint
{
    public sealed record UpdateCategoryRequest(
        string Name,
        int DisplayOrder,
        string? Color
    );

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("api/projects/{projectId:guid}/categories/{id:guid}", async (
            Guid projectId,
            Guid id,
            UpdateCategoryRequest request,
            ICommandHandler<UpdateCategoryCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new UpdateCategoryCommand(
                projectId,
                id,
                request.Name,
                request.DisplayOrder,
                request.Color);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(
                () => Results.NoContent(),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("Update Category")
        .WithDescription("Updates a category. Project owner/admins can update any category; members can update their own created categories.")
        .WithTags(Tags.Categories);
    }
}
