using PMS.SharedKernel;

namespace PMS.Domain.Users;

public sealed record UserCreatedDomainEvent(Guid UserId) : IDomainEvent;
