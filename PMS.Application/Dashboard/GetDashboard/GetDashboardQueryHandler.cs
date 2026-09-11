using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Caching;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.ActionItems;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.Dashboard.GetDashboard;

internal sealed class GetDashboardQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider,
    IApplicationCache? cache = null)
    : IQueryHandler<GetDashboardQuery, DashboardResponse>
{
    public async Task<Result<DashboardResponse>> Handle(
        GetDashboardQuery query,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure<DashboardResponse>(UserErrors.Unauthorized);
        }

        Guid userId = userContext.UserId.Value;
        bool isSystemAdmin = userContext.IsSystemAdmin;
        int pageNumber = CacheKeyBuilder.NormalizePageNumber(query.PageNumber);
        int pageSize = CacheKeyBuilder.NormalizePageSize(query.PageSize);
        DateOnly today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);
        IApplicationCache applicationCache = cache ?? NullApplicationCache.Instance;
        string key = CacheKeyBuilder.Create("dashboard", userId, isSystemAdmin, pageNumber, pageSize, today);

        return await applicationCache.GetOrCreateAsync(
            key,
            token => LoadPageAsync(userId, isSystemAdmin, pageNumber, pageSize, today, token),
            CachePolicy.Computed,
            [CacheTags.UserDashboard(userId), CacheTags.AllDashboards],
            cancellationToken);
    }

    private async ValueTask<DashboardResponse> LoadPageAsync(
        Guid userId,
        bool isSystemAdmin,
        int pageNumber,
        int pageSize,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        int skip = (int)Math.Min((long)(pageNumber - 1) * pageSize, int.MaxValue);

        var projectsQuery = context.Projects
            .AsNoTracking()
            .Where(p => isSystemAdmin ||
                        p.CreatedByUserId == userId ||
                        context.ProjectMembers.Any(pm =>
                            pm.ProjectId == p.Id && pm.UserId == userId));

        int totalCount = await projectsQuery.CountAsync(cancellationToken);

        List<ProjectDashboardReadModel> projects = await projectsQuery
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .Skip(skip)
            .Take(pageSize)
            .Select(p => new ProjectDashboardReadModel(
                p.Id,
                p.Name,
                p.Status,
                p.ProgressMode,
                p.StartDate,
                p.EndDate,
                p.CreatedByUserId))
            .ToListAsync(cancellationToken);

        if (projects.Count == 0)
        {
            return CreateResponse([], pageNumber, pageSize, totalCount);
        }

        List<Guid> projectIds = projects.Select(p => p.Id).ToList();
        Dictionary<Guid, UserRole> userRolesByProject = await context.ProjectMembers
            .AsNoTracking()
            .Where(pm => pm.UserId == userId && projectIds.Contains(pm.ProjectId))
            .ToDictionaryAsync(pm => pm.ProjectId, pm => pm.Role, cancellationToken);

        var actionItemsData = await (
            from ai in context.ActionItems.AsNoTracking()
            where projectIds.Contains(ai.ProjectId)
            join ps in context.PlannedSchedules.AsNoTracking() on ai.Id equals ps.ActionItemId into psGroup
            from ps in psGroup.DefaultIfEmpty()
            join ae in context.ActualExecutions.AsNoTracking() on ai.Id equals ae.ActionItemId into aeGroup
            from ae in aeGroup.DefaultIfEmpty()
            select new
            {
                ai.ProjectId,
                ai.Weight,
                PlannedEndDate = ps != null ? (DateOnly?)ps.PlannedEndDate : null,
                ActualStartDate = ae != null ? (DateOnly?)ae.ActualStartDate : null,
                ActualEndDate = ae != null ? (DateOnly?)ae.ActualEndDate : null
            }).ToListAsync(cancellationToken);

        var actionItemsByProject = actionItemsData
            .GroupBy(ai => ai.ProjectId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var projectSummaries = new List<DashboardProjectSummaryResponse>(projects.Count);

        foreach (ProjectDashboardReadModel project in projects)
        {
            actionItemsByProject.TryGetValue(project.Id, out var projectItems);
            projectItems ??= [];

            int completedCount = 0;
            int ongoingCount = 0;
            int delayedCount = 0;
            int plannedCount = 0;
            double totalWeight = 0;
            double completedWeight = 0;

            foreach (var item in projectItems)
            {
                double weight = (double)(item.Weight ?? 0m);
                totalWeight += weight;

                ActionItemStatus status = ActionItemStatusService.ComputeStatus(
                    item.PlannedEndDate,
                    item.ActualStartDate,
                    item.ActualEndDate,
                    today);

                switch (status)
                {
                    case ActionItemStatus.CompletedEarly:
                    case ActionItemStatus.CompletedOntime:
                    case ActionItemStatus.CompletedLate:
                        completedCount++;
                        completedWeight += weight;
                        break;
                    case ActionItemStatus.Ongoing:
                        ongoingCount++;
                        break;
                    case ActionItemStatus.Delayed:
                        delayedCount++;
                        break;
                    case ActionItemStatus.Plan:
                    default:
                        plannedCount++;
                        break;
                }
            }

            int totalItems = projectItems.Count;
            double progressPercent = project.ProgressMode == ProgressMode.WeightBased
                ? totalWeight > 0
                    ? Math.Round(completedWeight / totalWeight * 100.0, 2)
                    : 0.0
                : totalItems > 0
                    ? Math.Round((double)completedCount / totalItems * 100.0, 2)
                    : 0.0;

            string roleLabel = userRolesByProject.TryGetValue(project.Id, out UserRole userRole)
                ? userRole.ToString()
                : isSystemAdmin
                    ? "Admin"
                    : project.CreatedByUserId == userId
                        ? "ProjectManager"
                        : "Viewer";

            projectSummaries.Add(new DashboardProjectSummaryResponse(
                project.Id,
                project.Name,
                project.Status.ToString(),
                progressPercent,
                totalItems,
                completedCount,
                ongoingCount,
                delayedCount,
                plannedCount,
                project.StartDate,
                project.EndDate,
                roleLabel));
        }

        return CreateResponse(projectSummaries, pageNumber, pageSize, totalCount);
    }

    private static DashboardResponse CreateResponse(
        List<DashboardProjectSummaryResponse> projects,
        int pageNumber,
        int pageSize,
        int totalCount)
    {
        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new DashboardResponse(
            projects,
            pageNumber,
            pageSize,
            totalCount,
            totalPages,
            pageNumber > 1,
            pageNumber < totalPages);
    }

    private sealed record ProjectDashboardReadModel(
        Guid Id,
        string Name,
        ProjectStatus Status,
        ProgressMode ProgressMode,
        DateOnly StartDate,
        DateOnly EndDate,
        Guid? CreatedByUserId);
}
