using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.Projects.GetProjectById;

internal sealed class GetProjectByIdQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : IQueryHandler<GetProjectByIdQuery, ProjectResponse>
{
    public async Task<Result<ProjectResponse>> Handle(
        GetProjectByIdQuery query,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure<ProjectResponse>(UserErrors.Unauthorized);
        }

        ProjectResponse? project = await context.Projects
            .AsNoTracking()
            .Where(p => p.Id == query.Id)
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
            .SingleOrDefaultAsync(cancellationToken);

        if (project is null)
        {
            return Result.Failure<ProjectResponse>(ProjectErrors.NotFound(query.Id));
        }

        if (!userContext.IsSystemAdmin)
        {
            bool isMember = await context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == query.Id && pm.UserId == userContext.UserId.Value, cancellationToken);

            if (!isMember)
            {
                return Result.Failure<ProjectResponse>(ProjectErrors.NotProjectMember);
            }
        }

        return project;
    }
}
