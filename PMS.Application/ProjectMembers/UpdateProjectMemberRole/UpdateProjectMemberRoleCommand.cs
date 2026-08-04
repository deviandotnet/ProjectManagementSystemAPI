using PMS.Application.Abstractions.Messaging;
using PMS.Domain.Users;

namespace PMS.Application.ProjectMembers.UpdateProjectMemberRole;

public sealed record UpdateProjectMemberRoleCommand(
    Guid ProjectId,
    Guid UserId,
    UserRole Role
) : ICommand;
