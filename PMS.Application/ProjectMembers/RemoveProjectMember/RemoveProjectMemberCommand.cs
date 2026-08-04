using PMS.Application.Abstractions.Messaging;

namespace PMS.Application.ProjectMembers.RemoveProjectMember;

public sealed record RemoveProjectMemberCommand(
    Guid ProjectId,
    Guid UserId
) : ICommand;
