using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.ProjectMembers.GetProjectMembers;

internal sealed class GetProjectMembersQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : IQueryHandler<GetProjectMembersQuery, PagedResponse<ProjectMemberResponse>>
{
    public async Task<Result<PagedResponse<ProjectMemberResponse>>> Handle(
        GetProjectMembersQuery query,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure<PagedResponse<ProjectMemberResponse>>(UserErrors.Unauthorized);
        }

        bool projectExists = await context.Projects
            .AnyAsync(p => p.Id == query.ProjectId, cancellationToken);

        if (!projectExists)
        {
            return Result.Failure<PagedResponse<ProjectMemberResponse>>(ProjectErrors.NotFound(query.ProjectId));
        }

        if (!userContext.IsSystemAdmin)
        {
            bool isMember = await context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == query.ProjectId && pm.UserId == userContext.UserId.Value, cancellationToken);

            if (!isMember)
            {
                return Result.Failure<PagedResponse<ProjectMemberResponse>>(ProjectMemberErrors.NotProjectMember);
            }
        }

        int pageNumber = Math.Max(query.PageNumber, 1);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);
        int skip = (int)Math.Min((long)(pageNumber - 1) * pageSize, int.MaxValue);

        var membersQuery =
            from pm in context.ProjectMembers.AsNoTracking()
            join u in context.Users.AsNoTracking() on pm.UserId equals u.Id
            where pm.ProjectId == query.ProjectId
            select new { pm, u };

        int totalCount = await membersQuery.CountAsync(cancellationToken);

        List<ProjectMemberResponse> members = await membersQuery
            .OrderBy(x => x.pm.JoinedAt)
            .ThenBy(x => x.pm.Id)
            .Skip(skip)
            .Take(pageSize)
            .Select(x => new ProjectMemberResponse(
                x.pm.Id,
                x.pm.ProjectId,
                x.pm.UserId,
                x.u.FirstName,
                x.u.LastName,
                x.u.Email,
                x.pm.Role,
                x.pm.JoinedAt))
            .ToListAsync(cancellationToken);

        return PagedResponse<ProjectMemberResponse>.Create(
            members,
            pageNumber,
            pageSize,
            totalCount);
    }
}
