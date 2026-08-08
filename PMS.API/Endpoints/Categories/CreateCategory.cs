using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.Categories.CreateCategory;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.Categories;

internal sealed class CreateCategory : IApiEndpoint
{
    public sealed record CreateCategoryRequest(
        string Name,
        int DisplayOrder = 0,
        string? Color = null
    );

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/projects/{projectId:guid}/categories", async (
            Guid projectId,
            CreateCategoryRequest request,
            ICommandHandler<CreateCategoryCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateCategoryCommand(
                projectId,
                request.Name,
                request.DisplayOrder,
                request.Color);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                id => Results.Created($"/api/projects/{projectId}/categories/{id}", id),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("Create Category")
        .WithDescription("Creates a new project category. Requires project member permissions.")
        .WithTags(Tags.Categories);
    }
}
