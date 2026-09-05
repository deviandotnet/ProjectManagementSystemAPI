using PMS.SharedKernel;

namespace PMS.Domain.Users;

public sealed record RefreshTokenInvalidatedDomainEvent(
    Guid UserId,
    Guid RefreshTokenId) : IDomainEvent;
