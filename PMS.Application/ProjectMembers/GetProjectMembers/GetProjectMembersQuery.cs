using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;

namespace PMS.Application.ProjectMembers.GetProjectMembers;

public sealed record GetProjectMembersQuery(
    Guid ProjectId,
    int PageNumber = 1,
    int PageSize = 20)
    : IQuery<PagedResponse<ProjectMemberResponse>>;
