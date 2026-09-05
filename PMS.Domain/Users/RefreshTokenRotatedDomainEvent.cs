using PMS.SharedKernel;

namespace PMS.Domain.Users;

public sealed record RefreshTokenRotatedDomainEvent(
    Guid UserId,
    Guid PreviousRefreshTokenId,
    Guid NewRefreshTokenId) : IDomainEvent;
