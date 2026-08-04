using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.ProjectMembers.GetProjectMembers;

internal sealed class GetProjectMembersQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : IQueryHandler<GetProjectMembersQuery, List<ProjectMemberResponse>>
{
    public async Task<Result<List<ProjectMemberResponse>>> Handle(
        GetProjectMembersQuery query,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated)
        {
            return Result.Failure<List<ProjectMemberResponse>>(UserErrors.Unauthorized);
        }

        bool projectExists = await context.Projects
            .AnyAsync(p => p.Id == query.ProjectId, cancellationToken);

        if (!projectExists)
        {
            return Result.Failure<List<ProjectMemberResponse>>(ProjectErrors.NotFound(query.ProjectId));
        }

        List<ProjectMemberResponse> members = await (
            from pm in context.ProjectMembers.AsNoTracking()
            join u in context.Users.AsNoTracking() on pm.UserId equals u.Id
            where pm.ProjectId == query.ProjectId
            select new ProjectMemberResponse(
                pm.Id,
                pm.ProjectId,
                pm.UserId,
                u.FirstName,
                u.LastName,
                u.Email,
                pm.Role,
                pm.JoinedAt)
        ).ToListAsync(cancellationToken);

        return members;
    }
}
