using PMS.SharedKernel;

namespace PMS.Domain.Categories;

public sealed record CategoryCreatedDomainEvent(Guid CategoryId) : IDomainEvent;
public sealed record CategoryUpdatedDomainEvent(Guid CategoryId) : IDomainEvent;
public sealed record CategoryDeletedDomainEvent(Guid CategoryId) : IDomainEvent;
