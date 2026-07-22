using PMS.Application.Abstractions;
using PMS.Application.Constants;
using PMS.Application.Extensions;
using PMS.Domain.Abstractions;
using PMS.Domain.Abstractions.Errors;

namespace PMS.Application.Features.ProjectFeatures.CreateProject;

/// <summary>
/// Minimal API endpoint: POST /api/projects
/// Creates a new project in the system.
/// </summary>
public sealed class CreateProjectEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapPost("/api/projects", async (
            CreateProjectRequest request,
            IHandler<CreateProjectRequest, Result<CreateProjectResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(request, cancellationToken);

            return result.Match(
                onSuccess: project => Results.Created($"/api/projects/{project.Id}", project),
                onFailure: error => error.Type switch
                {
                    ErrorType.Conflict => Results.Conflict(error),
                    ErrorType.Validation => Results.BadRequest(error),
                    _ => Results.StatusCode(500)
                }
            );
        })
        .WithTags(ApiTags.Projects)
        .WithName("CreateProject")
        //.WithSummary("Creates a new project")
        .WithDescription("Creates a new project entity and returns the newly created project details. Returns 409 if project name already exists.")
        .Produces<CreateProjectResponse>(StatusCodes.Status201Created)
        .Produces<Error>(StatusCodes.Status400BadRequest)
        .Produces<Error>(StatusCodes.Status409Conflict);
    }
}
