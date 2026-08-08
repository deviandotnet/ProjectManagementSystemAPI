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

    public static readonly Error Forbidden = Error.Failure(
        "ProjectMembers.Forbidden",
        "You do not have permission to manage project members.");

    public static readonly Error NotProjectMember = Error.Failure(
        "ProjectMembers.NotProjectMember",
        "You must be a member of the project to access this resource.");
}
