using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.Users.GetUserById;

internal sealed class GetUserByIdQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext)
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

        UserResponse? user = await context.Users
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
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return Result.Failure<UserResponse>(UserErrors.NotFoundById(query.Id));
        }

        return user;
    }
}
