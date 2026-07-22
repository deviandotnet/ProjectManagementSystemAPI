using PMS.Domain.Abstractions.Errors;

namespace PMS.Application.Features.ProjectFeatures;

/// <summary>
/// Centralised error definitions for the Project feature slice.
/// Every conditional error returned by any Project handler is defined here
/// to ensure consistent error codes and descriptions across all Project endpoints.
/// 
/// Naming convention: {Entity}.{Operation} — e.g. "Project.NotFound"
/// </summary>
public static class ProjectErrors
{
    public static readonly Error InvalidId =
        Error.Validation("Project.InvalidId", "The provided Project ID is not a valid GUID format.");

    public static Error NotFound(Guid projectId) =>
        Error.NotFound("Project.NotFound", $"Project with ID '{projectId}' was not found.");

    public static readonly Error NoProjectsFound =
        Error.NotFound("Project.NoProjectsFound", "No projects were found.");

    public static Error NameAlreadyExists(string name) =>
        Error.Conflict("Project.NameAlreadyExists", $"A project with the name '{name}' already exists.");
}
