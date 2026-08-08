using PMS.SharedKernel;

namespace PMS.Domain.SubCategories;

public sealed record SubCategoryCreatedDomainEvent(Guid SubCategoryId) : IDomainEvent;
public sealed record SubCategoryUpdatedDomainEvent(Guid SubCategoryId) : IDomainEvent;
public sealed record SubCategoryDeletedDomainEvent(Guid SubCategoryId) : IDomainEvent;
