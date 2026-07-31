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
}
