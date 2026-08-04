using PMS.SharedKernel;

namespace PMS.Domain.ProjectMembers;

public static class ProjectMemberErrors
{
    public static Error NotFound(Guid projectId, Guid userId) => Error.NotFound(
        "ProjectMembers.NotFound",
        $"The user '{userId}' is not a member of project '{projectId}'.");

    public static Error AlreadyExists(Guid projectId, Guid userId) => Error.Conflict(
        "ProjectMembers.AlreadyExists",
        $"The user '{userId}' is already a member of project '{projectId}'.");

    public static readonly Error InvalidRole = Error.Problem(
        "ProjectMembers.InvalidRole",
        "The specified user role is invalid.");
}
