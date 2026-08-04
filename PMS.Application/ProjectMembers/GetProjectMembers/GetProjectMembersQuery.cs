using PMS.Application.Abstractions.Messaging;

namespace PMS.Application.ProjectMembers.GetProjectMembers;

public sealed record GetProjectMembersQuery(Guid ProjectId) : IQuery<List<ProjectMemberResponse>>;
