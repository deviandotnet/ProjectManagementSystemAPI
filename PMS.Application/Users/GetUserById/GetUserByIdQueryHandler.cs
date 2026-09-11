using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Caching;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.Users.GetUserById;

internal sealed class GetUserByIdQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IApplicationCache? cache = null)
    : IQueryHandler<GetUserByIdQuery, UserResponse>
{
    public async Task<Result<UserResponse>> Handle(
        GetUserByIdQuery query,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated)
        {
            return Result.Failure<UserResponse>(UserErrors.Unauthorized);
        }

        bool userExists = await context.Users.AnyAsync(u => u.Id == query.Id, cancellationToken);

        if (!userExists)
        {
            return Result.Failure<UserResponse>(UserErrors.NotFoundById(query.Id));
        }

        IApplicationCache applicationCache = cache ?? NullApplicationCache.Instance;
        string key = CacheKeyBuilder.Create("user-profile", query.Id);

        UserResponse? user = await applicationCache.GetOrCreateAsync(
            key,
            async token => await context.Users
                .AsNoTracking()
                .Where(u => u.Id == query.Id)
                .Select(u => new UserResponse(
                    u.Id,
                    u.FirstName,
                    u.MiddleName,
                    u.LastName,
                    u.Email,
                    u.SystemRole,
                    u.IsActive))
                .SingleOrDefaultAsync(token),
            CachePolicy.Entity,
            [CacheTags.UserProfile(query.Id)],
            cancellationToken);

        return user is null
            ? Result.Failure<UserResponse>(UserErrors.NotFoundById(query.Id))
            : user;
    }
}
