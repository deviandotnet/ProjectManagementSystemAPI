using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Domain.ProjectMembers;

public sealed record ProjectMemberAddedDomainEvent(Guid MemberId, Guid ProjectId, Guid UserId) : IDomainEvent;
public sealed record ProjectMemberRemovedDomainEvent(Guid MemberId, Guid ProjectId, Guid UserId) : IDomainEvent;
public sealed record ProjectMemberRoleUpdatedDomainEvent(Guid MemberId, Guid ProjectId, Guid UserId, UserRole NewRole) : IDomainEvent;
