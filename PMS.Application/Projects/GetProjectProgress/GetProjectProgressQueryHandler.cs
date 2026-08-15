using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.ActionItems;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.Projects.GetProjectProgress;

internal sealed class GetProjectProgressQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetProjectProgressQuery, ProjectProgressResponse>
{
    public async Task<Result<ProjectProgressResponse>> Handle(
        GetProjectProgressQuery query,
        CancellationToken cancellationToken)
    {
        // 1. Auth check
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure<ProjectProgressResponse>(UserErrors.Unauthorized);
        }

        Guid userId = userContext.UserId.Value;

        // 2. Fetch Project
        var project = await context.Projects
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == query.ProjectId, cancellationToken);

        if (project is null)
        {
            return Result.Failure<ProjectProgressResponse>(ProjectErrors.NotFound(query.ProjectId));
        }

        // 3. Authorization check (SystemAdmin or ProjectMember)
        if (!userContext.IsSystemAdmin)
        {
            bool isMember = await context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == query.ProjectId && pm.UserId == userId, cancellationToken);

            if (!isMember)
            {
                return Result.Failure<ProjectProgressResponse>(ProjectErrors.Forbidden);
            }
        }

        // 4. Load Action Items with PlannedSchedules and ActualExecutions
        var actionItemsData = await (
            from ai in context.ActionItems.AsNoTracking()
            where ai.ProjectId == query.ProjectId
            join ps in context.PlannedSchedules.AsNoTracking() on ai.Id equals ps.ActionItemId into psGroup
            from ps in psGroup.DefaultIfEmpty()
            join ae in context.ActualExecutions.AsNoTracking() on ai.Id equals ae.ActionItemId into aeGroup
            from ae in aeGroup.DefaultIfEmpty()
            select new
            {
                ai.Id,
                ai.Weight,
                PlannedEndDate = ps != null ? (DateOnly?)ps.PlannedEndDate : null,
                ActualStartDate = ae != null ? (DateOnly?)ae.ActualStartDate : null,
                ActualEndDate = ae != null ? (DateOnly?)ae.ActualEndDate : null
            }
        ).ToListAsync(cancellationToken);

        // 5. Compute Statuses and KPI Metrics
        DateOnly today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

        int totalItems = actionItemsData.Count;
        int completedCount = 0;
        int ongoingCount = 0;
        int delayedCount = 0;
        int plannedCount = 0;

        double totalWeight = 0;
        double completedWeight = 0;

        foreach (var item in actionItemsData)
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

        // 6. Compute Overall Progress Percentage based on Project.ProgressMode
        double progressPercent;
        if (project.ProgressMode == ProgressMode.WeightBased)
        {
            progressPercent = totalWeight > 0
                ? Math.Round((completedWeight / totalWeight) * 100.0, 2)
                : 0.0;
        }
        else
        {
            // Default: CountBased
            progressPercent = totalItems > 0
                ? Math.Round(((double)completedCount / totalItems) * 100.0, 2)
                : 0.0;
        }

        var response = new ProjectProgressResponse(
            project.Id,
            project.Name,
            project.ProgressMode,
            project.ProgressMode.ToString(),
            progressPercent,
            totalItems,
            completedCount,
            ongoingCount,
            delayedCount,
            plannedCount,
            totalWeight,
            completedWeight
        );

        return response;
    }
}
