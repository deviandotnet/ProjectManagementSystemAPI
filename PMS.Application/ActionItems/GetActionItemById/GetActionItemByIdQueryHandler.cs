using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.ActionItems;
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

        // ── 4. Fetch only fields required by the response ─────────────────
        ActionItemReadModel? rawItem = await (
            from ai in context.ActionItems.AsNoTracking()
            join c in context.Categories.AsNoTracking() on ai.CategoryId equals c.Id
            join sc in context.SubCategories.AsNoTracking() on ai.SubCategoryId equals sc.Id into scGroup
            from sc in scGroup.DefaultIfEmpty()
            join ps in context.PlannedSchedules.AsNoTracking() on ai.Id equals ps.ActionItemId into psGroup
            from ps in psGroup.DefaultIfEmpty()
            join ae in context.ActualExecutions.AsNoTracking() on ai.Id equals ae.ActionItemId into aeGroup
            from ae in aeGroup.DefaultIfEmpty()
            where ai.Id == query.ActionItemId && ai.ProjectId == query.ProjectId
            select new ActionItemReadModel(
                ai.Id,
                ai.ActionItemName,
                ai.CategoryId,
                c.Name,
                ai.SubCategoryId,
                sc == null ? null : sc.Name,
                (int)ai.Priority,
                ai.OwnerName,
                ai.Sequence,
                ps == null ? null : ps.Id,
                ps == null ? null : ps.PlannedStartDate,
                ps == null ? null : ps.PlannedEndDate,
                ps == null ? null : ps.PlannedStartWeek,
                ps == null ? null : ps.PlannedEndWeek,
                ps == null ? null : ps.DurationCalendarDays,
                ps == null ? null : ps.DurationWorkingDays,
                ae == null ? null : ae.Id,
                ae == null ? null : ae.ActualStartDate,
                ae == null ? null : ae.ActualEndDate,
                ae == null ? null : ae.ActualHours,
                ae == null ? null : ae.DelayReason,
                ai.Weight,
                ai.Remarks))
            .SingleOrDefaultAsync(cancellationToken);

        if (rawItem is null)
        {
            return Result.Failure<ActionItemResponse>(ActionItemErrors.NotFound(query.ActionItemId));
        }

        // ── 5. Dynamic Status Engine Computation ───────────────────────────
        DateOnly today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);
        ActionItemStatus status = ActionItemStatusService.ComputeStatus(
            rawItem.PlannedEndDate,
            rawItem.ActualStartDate,
            rawItem.ActualEndDate,
            today);

        return new ActionItemResponse(
            rawItem.Id,
            rawItem.ActionItemName,
            rawItem.CategoryId,
            rawItem.CategoryName,
            rawItem.SubCategoryId,
            rawItem.SubCategoryName,
            rawItem.Priority,
            rawItem.OwnerName,
            rawItem.Sequence,
            rawItem.PlannedScheduleId is null
                ? null
                : new PlannedScheduleResponse(
                    rawItem.PlannedScheduleId.Value,
                    rawItem.PlannedStartDate!.Value,
                    rawItem.PlannedEndDate!.Value,
                    rawItem.PlannedStartWeek!,
                    rawItem.PlannedEndWeek!,
                    rawItem.DurationCalendarDays!.Value,
                    rawItem.DurationWorkingDays!.Value),
            rawItem.ActualExecutionId is null
                ? null
                : new ActualExecutionResponse(
                    rawItem.ActualExecutionId.Value,
                    rawItem.ActualStartDate,
                    rawItem.ActualEndDate,
                    rawItem.ActualHours,
                    rawItem.DelayReason),
            (int)status,
            status.ToString(),
            rawItem.Weight,
            rawItem.Remarks);
    }

    private sealed record ActionItemReadModel(
        Guid Id,
        string ActionItemName,
        Guid CategoryId,
        string CategoryName,
        Guid? SubCategoryId,
        string? SubCategoryName,
        int Priority,
        string? OwnerName,
        int Sequence,
        Guid? PlannedScheduleId,
        DateOnly? PlannedStartDate,
        DateOnly? PlannedEndDate,
        string? PlannedStartWeek,
        string? PlannedEndWeek,
        int? DurationCalendarDays,
        int? DurationWorkingDays,
        Guid? ActualExecutionId,
        DateOnly? ActualStartDate,
        DateOnly? ActualEndDate,
        decimal? ActualHours,
        string? DelayReason,
        decimal? Weight,
        string? Remarks);
}
