using PMS.SharedKernel;

namespace PMS.Domain.Projects;

public sealed record ProjectUpdatedDomainEvent(Guid ProjectId) : IDomainEvent;
