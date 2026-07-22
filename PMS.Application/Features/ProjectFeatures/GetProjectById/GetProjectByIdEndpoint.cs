using PMS.Application.Abstractions;
using PMS.Application.Constants;
using PMS.Application.Extensions;
using PMS.Domain.Abstractions;
using PMS.Domain.Abstractions.Errors;

namespace PMS.Application.Features.ProjectFeatures.GetProjectById;

/// <summary>
/// Minimal API endpoint: GET /api/projects/{projectId}
/// Retrieves a single project by its ID.
/// 
/// The route does NOT use a :guid constraint — this allows the endpoint to receive
/// any string and return a meaningful 400 Bad Request with a message, instead of
/// ASP.NET silently returning a raw 404 for invalid GUID formats.
/// </summary>
public sealed class GetProjectByIdEndpoint : IApiEndpoint
{
    public void MapEndpoint(WebApplication app)
    {
        app.MapGet("/api/projects/{projectId}", async (
            string projectId,
            IHandler<GetProjectByIdRequest, Result<GetProjectByIdResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            // Parse at the endpoint level — gives us full control over the error message
            if (!Guid.TryParse(projectId, out var parsedId) || parsedId == Guid.Empty)
            {
                return Results.BadRequest(ProjectErrors.InvalidId);
            }

            var request = new GetProjectByIdRequest(parsedId);
            var result = await handler.HandleAsync(request, cancellationToken);

            return result.Match(
                onSuccess: project => Results.Ok(project),
                onFailure: error => error.Type switch
                {
                    ErrorType.NotFound => Results.NotFound(error),
                    ErrorType.Validation => Results.BadRequest(error),
                    _ => Results.StatusCode(500)
                }
            );
        })
        .WithTags(ApiTags.Projects)
        .WithName("GetProjectById")
        //.WithSummary("Retrieves a project by ID")
        .WithDescription("Returns a single project with full details including audit metadata. Returns 404 if not found, 400 if the ID format is invalid.")
        .Produces<GetProjectByIdResponse>(StatusCodes.Status200OK)
        .Produces<Error>(StatusCodes.Status404NotFound)
        .Produces<Error>(StatusCodes.Status400BadRequest);
    }
}
