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

namespace PMS.Application.ActionItems.GetActionItemById;

internal sealed class GetActionItemByIdQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetActionItemByIdQuery, ActionItemResponse>
{
    public async Task<Result<ActionItemResponse>> Handle(
        GetActionItemByIdQuery query,
        CancellationToken cancellationToken)
    {
        // ── 1. Auth Check ──────────────────────────────────────────────────
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure<ActionItemResponse>(UserErrors.Unauthorized);
        }

        Guid userId = userContext.UserId.Value;

        // ── 2. Project Existence Check ─────────────────────────────────────
        bool projectExists = await context.Projects
            .AnyAsync(p => p.Id == query.ProjectId, cancellationToken);

        if (!projectExists)
        {
            return Result.Failure<ActionItemResponse>(ProjectErrors.NotFound(query.ProjectId));
        }

        // ── 3. Project Membership Check ────────────────────────────────────
        if (!userContext.IsSystemAdmin)
        {
            bool isMember = await context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == query.ProjectId && pm.UserId == userId, cancellationToken);

            if (!isMember)
            {
                return Result.Failure<ActionItemResponse>(ActionItemErrors.NotProjectMember);
            }
        }

        // ── 4. Fetch Action Item with Left Joins ───────────────────────────
        var rawItem = await (from ai in context.ActionItems.AsNoTracking()
                             join c in context.Categories.AsNoTracking() on ai.CategoryId equals c.Id
                             join sc in context.SubCategories.AsNoTracking() on ai.SubCategoryId equals sc.Id into scGroup
                             from sc in scGroup.DefaultIfEmpty()
                             join ps in context.PlannedSchedules.AsNoTracking() on ai.Id equals ps.ActionItemId into psGroup
                             from ps in psGroup.DefaultIfEmpty()
                             join ae in context.ActualExecutions.AsNoTracking() on ai.Id equals ae.ActionItemId into aeGroup
                             from ae in aeGroup.DefaultIfEmpty()
                             where ai.Id == query.ActionItemId && ai.ProjectId == query.ProjectId
                             select new { ai, c, sc, ps, ae })
                            .SingleOrDefaultAsync(cancellationToken);

        if (rawItem is null)
        {
            return Result.Failure<ActionItemResponse>(ActionItemErrors.NotFound(query.ActionItemId));
        }

        // ── 5. Dynamic Status Engine Computation ───────────────────────────
        DateOnly today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);
        ActionItemStatus status = ComputeStatus(rawItem.ps, rawItem.ae, today);

        return new ActionItemResponse(
            rawItem.ai.Id,
            rawItem.ai.ActionItemName,
            rawItem.ai.CategoryId,
            rawItem.c.Name,
            rawItem.ai.SubCategoryId,
            rawItem.sc?.Name,
            (int)rawItem.ai.Priority,
            rawItem.ai.OwnerName,
            rawItem.ai.Sequence,
            rawItem.ps is null
                ? null
                : new PlannedScheduleResponse(
                    rawItem.ps.Id,
                    rawItem.ps.PlannedStartDate,
                    rawItem.ps.PlannedEndDate,
                    rawItem.ps.PlannedStartWeek,
                    rawItem.ps.PlannedEndWeek,
                    rawItem.ps.DurationCalendarDays,
                    rawItem.ps.DurationWorkingDays),
            rawItem.ae is null
                ? null
                : new ActualExecutionResponse(
                    rawItem.ae.Id,
                    rawItem.ae.ActualStartDate,
                    rawItem.ae.ActualEndDate,
                    rawItem.ae.ActualHours,
                    rawItem.ae.DelayReason),
            (int)status,
            status.ToString(),
            rawItem.ai.Weight,
            rawItem.ai.Remarks);
    }

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
