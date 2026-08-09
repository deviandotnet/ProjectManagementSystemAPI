using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.ActionItems;
using PMS.Domain.ActualExecutions;
using PMS.Domain.PlannedSchedules;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.ActionItems.GetActionItems;

internal sealed class GetActionItemsQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetActionItemsQuery, IReadOnlyCollection<ActionItemResponse>>
{
    public async Task<Result<IReadOnlyCollection<ActionItemResponse>>> Handle(
        GetActionItemsQuery query,
        CancellationToken cancellationToken)
    {
        // ── Auth check ─────────────────────────────────────────────────────
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure<IReadOnlyCollection<ActionItemResponse>>(UserErrors.Unauthorized);
        }

        Guid userId = userContext.UserId.Value;

        // ── Project existence check ────────────────────────────────────────
        bool projectExists = await context.Projects
            .AnyAsync(p => p.Id == query.ProjectId, cancellationToken);

        if (!projectExists)
        {
            return Result.Failure<IReadOnlyCollection<ActionItemResponse>>(ProjectErrors.NotFound(query.ProjectId));
        }

        // ── Project membership check ───────────────────────────────────────
        if (!userContext.IsSystemAdmin)
        {
            bool isMember = await context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == query.ProjectId && pm.UserId == userId, cancellationToken);

            if (!isMember)
            {
                return Result.Failure<IReadOnlyCollection<ActionItemResponse>>(ActionItemErrors.NotProjectMember);
            }
        }

        // ── Build base query with left joins ───────────────────────────────
        var dbQuery = from ai in context.ActionItems.AsNoTracking()
                      join c in context.Categories.AsNoTracking() on ai.CategoryId equals c.Id
                      join sc in context.SubCategories.AsNoTracking() on ai.SubCategoryId equals sc.Id into scGroup
                      from sc in scGroup.DefaultIfEmpty()
                      join ps in context.PlannedSchedules.AsNoTracking() on ai.Id equals ps.ActionItemId into psGroup
                      from ps in psGroup.DefaultIfEmpty()
                      join ae in context.ActualExecutions.AsNoTracking() on ai.Id equals ae.ActionItemId into aeGroup
                      from ae in aeGroup.DefaultIfEmpty()
                      where ai.ProjectId == query.ProjectId
                      select new { ai, c, sc, ps, ae };

        // ── Apply SQL-level filters ────────────────────────────────────────
        if (query.CategoryId.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.ai.CategoryId == query.CategoryId.Value);
        }

        if (query.SubCategoryId.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.ai.SubCategoryId == query.SubCategoryId.Value);
        }

        if (query.Priority.HasValue)
        {
            dbQuery = dbQuery.Where(x => (int)x.ai.Priority == query.Priority.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.OwnerName))
        {
            dbQuery = dbQuery.Where(x => x.ai.OwnerName != null &&
                                         x.ai.OwnerName.Contains(query.OwnerName));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            string search = query.Search;
            dbQuery = dbQuery.Where(x =>
                x.ai.ActionItemName.Contains(search) ||
                (x.ai.Description != null && x.ai.Description.Contains(search)) ||
                (x.ai.OwnerName != null && x.ai.OwnerName.Contains(search)));
        }

        if (query.StartDate.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.ps != null && x.ps.PlannedStartDate >= query.StartDate.Value);
        }

        if (query.EndDate.HasValue)
        {
            dbQuery = dbQuery.Where(x => x.ps != null && x.ps.PlannedEndDate <= query.EndDate.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.WeekStart))
        {
            string weekStart = query.WeekStart;
            dbQuery = dbQuery.Where(x => x.ps != null &&
                                         string.Compare(x.ps.PlannedStartWeek, weekStart) >= 0);
        }

        if (!string.IsNullOrWhiteSpace(query.WeekEnd))
        {
            string weekEnd = query.WeekEnd;
            dbQuery = dbQuery.Where(x => x.ps != null &&
                                         string.Compare(x.ps.PlannedEndWeek, weekEnd) <= 0);
        }

        // ── Execute query ──────────────────────────────────────────────────
        var rawItems = await dbQuery
            .OrderBy(x => x.ai.Sequence)
            .ToListAsync(cancellationToken);

        // ── Compute status & map to response DTOs ──────────────────────────
        DateOnly today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

        IEnumerable<ActionItemResponse> results = rawItems.Select(x =>
        {
            ActionItemStatus status = ComputeStatus(x.ps, x.ae, today);

            return new ActionItemResponse(
                x.ai.Id,
                x.ai.ActionItemName,
                x.ai.CategoryId,
                x.c.Name,
                x.ai.SubCategoryId,
                x.sc?.Name,
                (int)x.ai.Priority,
                x.ai.OwnerName,
                x.ai.Sequence,
                x.ps is null
                    ? null
                    : new PlannedScheduleResponse(
                        x.ps.Id,
                        x.ps.PlannedStartDate,
                        x.ps.PlannedEndDate,
                        x.ps.PlannedStartWeek,
                        x.ps.PlannedEndWeek,
                        x.ps.DurationCalendarDays,
                        x.ps.DurationWorkingDays),
                x.ae is null
                    ? null
                    : new ActualExecutionResponse(
                        x.ae.Id,
                        x.ae.ActualStartDate,
                        x.ae.ActualEndDate,
                        x.ae.ActualHours,
                        x.ae.DelayReason),
                (int)status,
                status.ToString(),
                x.ai.Weight,
                x.ai.Remarks);
        });

        // ── Filter by computed status (post-query) ─────────────────────────
        if (query.Statuses is { Length: > 0 })
        {
            results = results.Where(r => query.Statuses.Contains(r.ComputedStatus));
        }

        return results.ToList();
    }

    /// <summary>
    /// Computes the ActionItemStatus at runtime based on the Status Engine rules
    /// defined in schema.md. Status is NEVER stored in the database.
    /// </summary>
    private static ActionItemStatus ComputeStatus(
        PlannedSchedule? planned,
        ActualExecution? actual,
        DateOnly today)
    {
        if (planned is null)
        {
            return ActionItemStatus.Plan;
        }

        if (actual?.ActualEndDate is not null)
        {
            if (actual.ActualEndDate < planned.PlannedEndDate)
                return ActionItemStatus.CompletedEarly;

            if (actual.ActualEndDate == planned.PlannedEndDate)
                return ActionItemStatus.CompletedOntime;

            return ActionItemStatus.CompletedLate;
        }

        if (actual?.ActualStartDate is not null)
        {
            return ActionItemStatus.Ongoing;
        }

        if (today > planned.PlannedEndDate)
        {
            return ActionItemStatus.Delayed;
        }

        return ActionItemStatus.Plan;
    }
}
