using Microsoft.EntityFrameworkCore;
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
    : IQueryHandler<GetProjectsByUserIdQuery, List<ProjectResponse>>
{
    public async Task<Result<List<ProjectResponse>>> Handle(
        GetProjectsByUserIdQuery query,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure<List<ProjectResponse>>(UserErrors.Unauthorized);
        }

        bool userExists = await context.Users
            .AnyAsync(u => u.Id == query.UserId, cancellationToken);

        if (!userExists)
        {
            return Result.Failure<List<ProjectResponse>>(UserErrors.NotFoundById(query.UserId));
        }

        List<ProjectResponse> projects;

        if (!userContext.IsSystemAdmin && query.UserId != userContext.UserId.Value)
        {
            return Result.Failure<List<ProjectResponse>>(ProjectErrors.Forbidden);
        }

        projects = await context.Projects
            .AsNoTracking()
            .Where(p => p.CreatedByUserId == query.UserId ||
                        context.ProjectMembers.Any(pm => pm.ProjectId == p.Id && pm.UserId == query.UserId))
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

        return projects;
    }
}
