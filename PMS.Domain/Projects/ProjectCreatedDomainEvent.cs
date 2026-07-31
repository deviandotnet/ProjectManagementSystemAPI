using PMS.SharedKernel;

namespace PMS.Domain.Projects;

public sealed record ProjectCreatedDomainEvent(Guid ProjectId) : IDomainEvent;
