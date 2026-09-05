using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.ActionItems;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.ActionItems.GetActionItems;

internal sealed class GetActionItemsQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetActionItemsQuery, PagedResponse<ActionItemResponse>>
{
    public async Task<Result<PagedResponse<ActionItemResponse>>> Handle(
        GetActionItemsQuery query,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure<PagedResponse<ActionItemResponse>>(UserErrors.Unauthorized);
        }

        Guid userId = userContext.UserId.Value;

        bool projectExists = await context.Projects
            .AnyAsync(p => p.Id == query.ProjectId, cancellationToken);

        if (!projectExists)
        {
            return Result.Failure<PagedResponse<ActionItemResponse>>(ProjectErrors.NotFound(query.ProjectId));
        }

        if (!userContext.IsSystemAdmin)
        {
            bool isMember = await context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == query.ProjectId && pm.UserId == userId, cancellationToken);

            if (!isMember)
            {
                return Result.Failure<PagedResponse<ActionItemResponse>>(ActionItemErrors.NotProjectMember);
            }
        }

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

        DateOnly today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);

        if (query.Statuses is { Length: > 0 })
        {
            bool includePlan = query.Statuses.Contains((int)ActionItemStatus.Plan);
            bool includeOngoing = query.Statuses.Contains((int)ActionItemStatus.Ongoing);
            bool includeDelayed = query.Statuses.Contains((int)ActionItemStatus.Delayed);
            bool includeCompletedEarly = query.Statuses.Contains((int)ActionItemStatus.CompletedEarly);
            bool includeCompletedOntime = query.Statuses.Contains((int)ActionItemStatus.CompletedOntime);
            bool includeCompletedLate = query.Statuses.Contains((int)ActionItemStatus.CompletedLate);

            dbQuery = dbQuery.Where(x =>
                (includePlan &&
                    (x.ps == null ||
                     (x.ae == null || (!x.ae.ActualStartDate.HasValue && !x.ae.ActualEndDate.HasValue)) &&
                     today <= x.ps.PlannedEndDate)) ||
                (includeOngoing && x.ps != null && x.ae != null &&
                    x.ae.ActualStartDate.HasValue && !x.ae.ActualEndDate.HasValue) ||
                (includeDelayed && x.ps != null &&
                    (x.ae == null || (!x.ae.ActualStartDate.HasValue && !x.ae.ActualEndDate.HasValue)) &&
                    today > x.ps.PlannedEndDate) ||
                (includeCompletedEarly && x.ps != null && x.ae != null &&
                    x.ae.ActualEndDate.HasValue && x.ae.ActualEndDate < x.ps.PlannedEndDate) ||
                (includeCompletedOntime && x.ps != null && x.ae != null &&
                    x.ae.ActualEndDate.HasValue && x.ae.ActualEndDate == x.ps.PlannedEndDate) ||
                (includeCompletedLate && x.ps != null && x.ae != null &&
                    x.ae.ActualEndDate.HasValue && x.ae.ActualEndDate > x.ps.PlannedEndDate));
        }

        int pageNumber = Math.Max(query.PageNumber, 1);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);
        int itemsToSkip = (int)Math.Min((long)(pageNumber - 1) * pageSize, int.MaxValue);
        int totalCount = await dbQuery.CountAsync(cancellationToken);

        List<ActionItemReadModel> rawItems = await dbQuery
            .OrderBy(x => x.ai.Sequence)
            .ThenBy(x => x.ai.Id)
            .Skip(itemsToSkip)
            .Take(pageSize)
            .Select(x => new ActionItemReadModel(
                x.ai.Id,
                x.ai.ActionItemName,
                x.ai.CategoryId,
                x.c.Name,
                x.ai.SubCategoryId,
                x.sc == null ? null : x.sc.Name,
                (int)x.ai.Priority,
                x.ai.OwnerName,
                x.ai.Sequence,
                x.ps == null ? null : x.ps.Id,
                x.ps == null ? null : x.ps.PlannedStartDate,
                x.ps == null ? null : x.ps.PlannedEndDate,
                x.ps == null ? null : x.ps.PlannedStartWeek,
                x.ps == null ? null : x.ps.PlannedEndWeek,
                x.ps == null ? null : x.ps.DurationCalendarDays,
                x.ps == null ? null : x.ps.DurationWorkingDays,
                x.ae == null ? null : x.ae.Id,
                x.ae == null ? null : x.ae.ActualStartDate,
                x.ae == null ? null : x.ae.ActualEndDate,
                x.ae == null ? null : x.ae.ActualHours,
                x.ae == null ? null : x.ae.DelayReason,
                x.ai.Weight,
                x.ai.Remarks))
            .ToListAsync(cancellationToken);

        List<ActionItemResponse> items = rawItems.Select(item =>
        {
            ActionItemStatus status = ActionItemStatusService.ComputeStatus(
                item.PlannedEndDate,
                item.ActualStartDate,
                item.ActualEndDate,
                today);

            return new ActionItemResponse(
                item.Id,
                item.ActionItemName,
                item.CategoryId,
                item.CategoryName,
                item.SubCategoryId,
                item.SubCategoryName,
                item.Priority,
                item.OwnerName,
                item.Sequence,
                item.PlannedScheduleId is null
                    ? null
                    : new PlannedScheduleResponse(
                        item.PlannedScheduleId.Value,
                        item.PlannedStartDate!.Value,
                        item.PlannedEndDate!.Value,
                        item.PlannedStartWeek!,
                        item.PlannedEndWeek!,
                        item.DurationCalendarDays!.Value,
                        item.DurationWorkingDays!.Value),
                item.ActualExecutionId is null
                    ? null
                    : new ActualExecutionResponse(
                        item.ActualExecutionId.Value,
                        item.ActualStartDate,
                        item.ActualEndDate,
                        item.ActualHours,
                        item.DelayReason),
                (int)status,
                status.ToString(),
                item.Weight,
                item.Remarks);
        }).ToList();

        return PagedResponse<ActionItemResponse>.Create(items, pageNumber, pageSize, totalCount);
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
