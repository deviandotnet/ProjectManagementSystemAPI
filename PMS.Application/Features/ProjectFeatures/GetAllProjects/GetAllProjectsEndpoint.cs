using PMS.Application.Abstractions;
using PMS.Application.Constants;
using PMS.Application.Extensions;
using PMS.Domain.Abstractions;
using PMS.Domain.Abstractions.Errors;

namespace PMS.Application.Features.ProjectFeatures.GetAllProjects;

/// <summary>
/// Minimal API endpoint: GET /api/projects
/// Retrieves all projects as a flat list.
/// </summary>
public sealed class GetAllProjectsEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapGet("/api/projects", async (
            IHandler<Unit, Result<IEnumerable<GetAllProjectsResponse>>> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(new Unit(), cancellationToken);

            return result.Match(
                onSuccess: projects => Results.Ok(projects),
                onFailure: error => error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(error),
                    _ => Results.StatusCode(500)
                }
            );
        })
        .WithTags(ApiTags.Projects)
        .WithName("GetAllProjects")
        //.WithSummary("Retrieves all projects")
        .WithDescription("Returns a list of all projects with their settings. Returns 404 if no projects exist.")
        .Produces<IEnumerable<GetAllProjectsResponse>>(StatusCodes.Status200OK)
        .Produces<Error>(StatusCodes.Status404NotFound);
    }
}
