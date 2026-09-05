using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.Projects.GetProjectsByUserId;

internal sealed class GetProjectsByUserIdQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : IQueryHandler<GetProjectsByUserIdQuery, PagedResponse<ProjectResponse>>
{
    public async Task<Result<PagedResponse<ProjectResponse>>> Handle(
        GetProjectsByUserIdQuery query,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure<PagedResponse<ProjectResponse>>(UserErrors.Unauthorized);
        }

        bool userExists = await context.Users
            .AnyAsync(u => u.Id == query.UserId, cancellationToken);

        if (!userExists)
        {
            return Result.Failure<PagedResponse<ProjectResponse>>(UserErrors.NotFoundById(query.UserId));
        }

        if (!userContext.IsSystemAdmin && query.UserId != userContext.UserId.Value)
        {
            return Result.Failure<PagedResponse<ProjectResponse>>(ProjectErrors.Forbidden);
        }

        int pageNumber = Math.Max(query.PageNumber, 1);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);
        int skip = (int)Math.Min((long)(pageNumber - 1) * pageSize, int.MaxValue);

        var projectsQuery = context.Projects
            .AsNoTracking()
            .Where(p => p.CreatedByUserId == query.UserId ||
                        context.ProjectMembers.Any(pm => pm.ProjectId == p.Id && pm.UserId == query.UserId));

        int totalCount = await projectsQuery.CountAsync(cancellationToken);

        List<ProjectResponse> projects = await projectsQuery
            .OrderByDescending(p => p.CreatedAt)
            .ThenBy(p => p.Id)
            .Skip(skip)
            .Take(pageSize)
            .Select(p => new ProjectResponse(
                p.Id,
                p.Name,
                p.Description,
                p.StartDate,
                p.EndDate,
                p.WeekStartDay,
                p.DefaultTimelineScale,
                p.ProgressMode,
                p.Status,
                p.CreatedByUserId))
            .ToListAsync(cancellationToken);

        return PagedResponse<ProjectResponse>.Create(
            projects,
            pageNumber,
            pageSize,
            totalCount);
    }
}
