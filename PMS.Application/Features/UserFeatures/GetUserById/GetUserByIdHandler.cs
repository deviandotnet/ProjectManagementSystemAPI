using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Data;
using PMS.Domain.Abstractions;

namespace PMS.Application.Features.UserFeatures.GetUserById;

/// <summary>
/// Handler for retrieving a single user by ID.
/// 
/// Request: GetUserByIdRequest
/// Response: Result&lt;GetUserByIdResponse&gt;
/// </summary>
public sealed class GetUserByIdHandler(IApplicationDbContext dbContext)
    : IHandler<GetUserByIdRequest, Result<GetUserByIdResponse>>
{
    public async Task<Result<GetUserByIdResponse>> HandleAsync(
        GetUserByIdRequest command,
        CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == command.UserId)
            .Select(u => new GetUserByIdResponse(
                u.Id,
                u.FirstName,
                u.MiddleName,
                u.LastName,
                u.Email,
                u.IsActive,
                u.CreatedByUserId,
                u.CreatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return UserErrors.NotFound(command.UserId);
        }

        return Result.Success(user);
    }
}
