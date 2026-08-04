using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.Users;
using PMS.SharedKernel;
using System;
using System.Collections.Generic;
using PMS.Application.Users;

namespace PMS.Application.Users.LoginUser
{
    internal sealed class LoginUserCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher hasher,
        ITokenProvider tokenProvider,
        IDateTimeProvider dateTimeProvider) : ICommandHandler<LoginUserCommand, AccessTokenResponse>
    {
        public async Task<Result<AccessTokenResponse>> Handle(LoginUserCommand command, CancellationToken cancellationToken)
        {
            var user = await context.Users
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.Email == command.Email, cancellationToken);

            if(user is null)
            {
                return Result.Failure<AccessTokenResponse>(UserErrors.NotFoundByEmail);
            }

            var verified = hasher.Verify(command.Password, user.PasswordHash);

            if (!verified)
            {
                return Result.Failure<AccessTokenResponse>(UserErrors.NotFoundByEmail);
            }

            string accessToken = tokenProvider.CreateAccessToken(user);
            string refreshToken = tokenProvider.CreateRefreshToken();

            var refreshTokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                Token = refreshToken,
                UserId = user.Id,
                ExpiresOnUtc = dateTimeProvider.UtcNow.AddDays(7)
            };

            await context.RefreshTokens.AddAsync(refreshTokenEntity, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);


            return new AccessTokenResponse(accessToken, refreshToken);

        }
    }
}
