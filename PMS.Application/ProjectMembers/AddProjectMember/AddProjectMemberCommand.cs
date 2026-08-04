using PMS.Application.Abstractions.Messaging;
using PMS.Domain.Users;

namespace PMS.Application.ProjectMembers.AddProjectMember;

public sealed record AddProjectMemberCommand(
    Guid ProjectId,
    Guid UserId,
    UserRole Role
) : ICommand;
