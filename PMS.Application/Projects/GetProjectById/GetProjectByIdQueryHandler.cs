using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Caching;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.Projects.GetProjectById;

internal sealed class GetProjectByIdQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IApplicationCache? cache = null)
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

        bool projectExists = await context.Projects
            .AnyAsync(p => p.Id == query.Id, cancellationToken);

        if (!projectExists)
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

        IApplicationCache applicationCache = cache ?? NullApplicationCache.Instance;
        string key = CacheKeyBuilder.Create("project-details", query.Id);

        ProjectResponse? project = await applicationCache.GetOrCreateAsync(
            key,
            async token => await context.Projects
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
                .SingleOrDefaultAsync(token),
            CachePolicy.Entity,
            [CacheTags.Project(query.Id), CacheTags.ProjectDetails(query.Id)],
            cancellationToken);

        return project is null
            ? Result.Failure<ProjectResponse>(ProjectErrors.NotFound(query.Id))
            : project;
    }
}
