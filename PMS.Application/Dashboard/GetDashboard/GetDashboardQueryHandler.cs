using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
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
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetDashboardQuery, DashboardResponse>
{
    public async Task<Result<DashboardResponse>> Handle(
        GetDashboardQuery query,
        CancellationToken cancellationToken)
    {
        // 1. Auth check
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure<DashboardResponse>(UserErrors.Unauthorized);
        }

        Guid userId = userContext.UserId.Value;
        bool isSystemAdmin = userContext.IsSystemAdmin;

        // 2. Query user's projects and member roles
        List<Project> projects;
        Dictionary<Guid, UserRole> userRolesByProject = new();

        if (isSystemAdmin)
        {
            projects = await context.Projects
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);

            var members = await context.ProjectMembers
                .AsNoTracking()
                .Where(pm => pm.UserId == userId)
                .ToListAsync(cancellationToken);

            foreach (var m in members)
            {
                userRolesByProject[m.ProjectId] = m.Role;
            }
        }
        else
        {
            var userMemberships = await context.ProjectMembers
                .AsNoTracking()
                .Where(pm => pm.UserId == userId)
                .ToListAsync(cancellationToken);

            foreach (var m in userMemberships)
            {
                userRolesByProject[m.ProjectId] = m.Role;
            }

            var memberProjectIds = userMemberships.Select(m => m.ProjectId).ToList();

            projects = await context.Projects
                .AsNoTracking()
                .Where(p => memberProjectIds.Contains(p.Id) || p.CreatedByUserId == userId)
                .OrderBy(p => p.Name)
                .ToListAsync(cancellationToken);
        }

        if (projects.Count == 0)
        {
            return new DashboardResponse([]);
        }

        var projectIds = projects.Select(p => p.Id).ToList();

        // 3. Load Action Items for all user projects in a single query
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
                ai.Id,
                ai.Weight,
                PlannedEndDate = ps != null ? (DateOnly?)ps.PlannedEndDate : null,
                ActualStartDate = ae != null ? (DateOnly?)ae.ActualStartDate : null,
                ActualEndDate = ae != null ? (DateOnly?)ae.ActualEndDate : null
            }
        ).ToListAsync(cancellationToken);

        var actionItemsByProject = actionItemsData
            .GroupBy(ai => ai.ProjectId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 4. Compute KPIs per project
        DateOnly today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);
        var projectSummaries = new List<DashboardProjectSummaryResponse>();

        foreach (var project in projects)
        {
            actionItemsByProject.TryGetValue(project.Id, out var projectItems);
            projectItems ??= [];

            int totalItems = projectItems.Count;
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

            // Calculate progress percent based on ProgressMode
            double progressPercent;
            if (project.ProgressMode == ProgressMode.WeightBased)
            {
                progressPercent = totalWeight > 0
                    ? Math.Round((completedWeight / totalWeight) * 100.0, 2)
                    : 0.0;
            }
            else
            {
                progressPercent = totalItems > 0
                    ? Math.Round(((double)completedCount / totalItems) * 100.0, 2)
                    : 0.0;
            }

            // Determine user role string
            string roleLabel;
            if (userRolesByProject.TryGetValue(project.Id, out var userRole))
            {
                roleLabel = userRole.ToString();
            }
            else if (isSystemAdmin)
            {
                roleLabel = "Admin";
            }
            else if (project.CreatedByUserId == userId)
            {
                roleLabel = "ProjectManager";
            }
            else
            {
                roleLabel = "Viewer";
            }

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
                roleLabel
            ));
        }

        return new DashboardResponse(projectSummaries);
    }
}
