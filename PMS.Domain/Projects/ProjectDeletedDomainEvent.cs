using PMS.SharedKernel;

namespace PMS.Domain.Projects;

public sealed record ProjectDeletedDomainEvent(Guid ProjectId) : IDomainEvent;
