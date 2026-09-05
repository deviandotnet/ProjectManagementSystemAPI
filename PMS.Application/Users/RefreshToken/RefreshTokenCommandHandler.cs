using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.Users;
using PMS.SharedKernel;
using RefreshTokenEntity = PMS.Domain.Users.RefreshToken;

namespace PMS.Application.Users.RefreshToken;

internal sealed class RefreshTokenCommandHandler(
    IApplicationDbContext context,
    IRepository<RefreshTokenEntity> refreshTokenRepository,
    IUnitOfWork unitOfWork,
    ITokenProvider tokenProvider,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<RefreshTokenCommand, AccessTokenResponse>
{
    public async Task<Result<AccessTokenResponse>> Handle(
        RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        RefreshTokenEntity? existingToken = await context.RefreshTokens
            .Include(refreshToken => refreshToken.User)
            .SingleOrDefaultAsync(
                refreshToken => refreshToken.Token == command.Token,
                cancellationToken);

        if (existingToken is null)
        {
            return Result.Failure<AccessTokenResponse>(UserErrors.InvalidRefreshToken);
        }

        User? user = existingToken.User;

        if (user is null)
        {
            await refreshTokenRepository.DeleteAsync(existingToken, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result.Failure<AccessTokenResponse>(UserErrors.InvalidRefreshToken);
        }

        if (existingToken.ExpiresOnUtc <= dateTimeProvider.UtcNow || !user.IsActive)
        {
            user.Raise(new RefreshTokenInvalidatedDomainEvent(user.Id, existingToken.Id));

            await refreshTokenRepository.DeleteAsync(existingToken, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            return Result.Failure<AccessTokenResponse>(UserErrors.InvalidRefreshToken);
        }

        string accessToken = tokenProvider.CreateAccessToken(user);
        string newRefreshTokenValue = tokenProvider.CreateRefreshToken();

        var newRefreshToken = new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            Token = newRefreshTokenValue,
            UserId = user.Id,
            ExpiresOnUtc = dateTimeProvider.UtcNow.AddDays(7)
        };

        await refreshTokenRepository.DeleteAsync(existingToken, cancellationToken);
        await refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

        user.Raise(new RefreshTokenRotatedDomainEvent(
            user.Id,
            existingToken.Id,
            newRefreshToken.Id));

        await unitOfWork.CommitAsync(cancellationToken);

        return new AccessTokenResponse(accessToken, newRefreshTokenValue);
    }
}
