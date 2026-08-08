using PMS.SharedKernel;

namespace PMS.Domain.Projects;

public static class ProjectErrors
{
    public static Error NotFound(Guid projectId) => Error.NotFound(
        "Projects.NotFound",
        $"The project with Id = '{projectId}' was not found.");

    public static Error NameAlreadyExists(string name) => Error.Conflict(
        "Projects.NameAlreadyExists",
        $"A project with the name '{name}' already exists.");

    public static readonly Error InvalidDates = Error.Problem(
        "Projects.InvalidDates",
        "The project end date must be on or after the start date.");

    public static readonly Error Forbidden = Error.Failure(
        "Projects.Forbidden",
        "You do not have permission to perform this action on the project.");

    public static readonly Error NotProjectMember = Error.Failure(
        "Projects.NotProjectMember",
        "You must be a member of the project to access this resource.");

    public static readonly Error AdminOnly = Error.Failure(
        "Projects.AdminOnly",
        "Only system administrators can perform this action.");
}
