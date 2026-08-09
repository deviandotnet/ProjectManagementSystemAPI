using PMS.SharedKernel;

namespace PMS.Domain.ActionItems;

public sealed record ActionItemCreatedDomainEvent(Guid ActionItemId, Guid ProjectId) : IDomainEvent;
public sealed record ActionItemUpdatedDomainEvent(Guid ActionItemId, Guid ProjectId) : IDomainEvent;
public sealed record ActionItemDeletedDomainEvent(Guid ActionItemId, Guid ProjectId) : IDomainEvent;
