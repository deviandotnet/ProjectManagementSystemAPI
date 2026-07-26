using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Data;
using PMS.Domain.Abstractions;

namespace PMS.Application.Features.UserFeatures.GetAllUsers;

/// <summary>
/// Handler for retrieving all users.
/// 
/// Request: GetAllUsersRequest
/// Response: Result&lt;IEnumerable&lt;GetAllUsersResponse&gt;&gt;
/// </summary>
public sealed class GetAllUsersHandler(IApplicationDbContext dbContext)
    : IHandler<GetAllUsersRequest, Result<IEnumerable<GetAllUsersResponse>>>
{
    public async Task<Result<IEnumerable<GetAllUsersResponse>>> HandleAsync(
        GetAllUsersRequest command,
        CancellationToken cancellationToken)
    {
        var users = await dbContext.Users
            .AsNoTracking()
            .Select(u => new GetAllUsersResponse(
                u.Id,
                u.FirstName,
                u.MiddleName,
                u.LastName,
                u.Email,
                u.IsActive,
                u.CreatedByUserId,
                u.CreatedAt
            ))
            .ToListAsync(cancellationToken);

        if (users.Count == 0)
        {
            return UserErrors.NoUsersFound;
        }

        return Result.Success<IEnumerable<GetAllUsersResponse>>(users);
    }
}
