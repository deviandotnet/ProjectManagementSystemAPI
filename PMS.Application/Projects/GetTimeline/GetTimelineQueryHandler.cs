using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.ActionItems;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.Projects.GetTimeline;

internal sealed class GetTimelineQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : IQueryHandler<GetTimelineQuery, TimelineResponse>
{
    public async Task<Result<TimelineResponse>> Handle(
        GetTimelineQuery query,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure<TimelineResponse>(UserErrors.Unauthorized);
        }

        Guid userId = userContext.UserId.Value;

        Project? project = await context.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == query.ProjectId, cancellationToken);

        if (project is null)
        {
            return Result.Failure<TimelineResponse>(ProjectErrors.NotFound(query.ProjectId));
        }

        if (!userContext.IsSystemAdmin)
        {
            bool isMember = await context.ProjectMembers
                .AsNoTracking()
                .AnyAsync(pm => pm.ProjectId == query.ProjectId && pm.UserId == userId, cancellationToken);

            if (!isMember)
            {
                return Result.Failure<TimelineResponse>(ActionItemErrors.NotProjectMember);
            }
        }

        TimelineScale scale = query.Scale ?? project.DefaultTimelineScale;
        DateOnly startDate = query.StartDate ?? project.StartDate;
        DateOnly endDate = query.EndDate ?? project.EndDate;

        if (endDate < startDate)
        {
            endDate = startDate.AddDays(30);
        }

        List<TimelineColumnResponse> columns = GenerateColumns(
            scale,
            startDate,
            endDate,
            project.WeekStartDay);

        var actionItemsQuery =
            from ai in context.ActionItems.AsNoTracking()
            join c in context.Categories.AsNoTracking() on ai.CategoryId equals c.Id
            join sc in context.SubCategories.AsNoTracking() on ai.SubCategoryId equals sc.Id into scGroup
            from sc in scGroup.DefaultIfEmpty()
            join ps in context.PlannedSchedules.AsNoTracking() on ai.Id equals ps.ActionItemId into psGroup
            from ps in psGroup.DefaultIfEmpty()
            join ae in context.ActualExecutions.AsNoTracking() on ai.Id equals ae.ActionItemId into aeGroup
            from ae in aeGroup.DefaultIfEmpty()
            where ai.ProjectId == query.ProjectId
            select new { ai, c, sc, ps, ae };

        int pageNumber = Math.Max(query.PageNumber, 1);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);
        int itemsToSkip = (int)Math.Min((long)(pageNumber - 1) * pageSize, int.MaxValue);
        int totalCount = await actionItemsQuery.CountAsync(cancellationToken);
        int totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        List<TimelineItemReadModel> actionItems = await actionItemsQuery
            .OrderBy(x => x.c.DisplayOrder)
            .ThenBy(x => x.c.Id)
            .ThenBy(x => x.ai.SubCategoryId.HasValue)
            .ThenBy(x => x.sc == null ? 0 : x.sc.DisplayOrder)
            .ThenBy(x => x.ai.SubCategoryId)
            .ThenBy(x => x.ai.Sequence)
            .ThenBy(x => x.ai.Id)
            .Skip(itemsToSkip)
            .Take(pageSize)
            .Select(x => new TimelineItemReadModel(
                x.ai.Id,
                x.ai.ActionItemName,
                x.ai.CategoryId,
                x.c.Name,
                x.c.Color,
                x.ai.SubCategoryId,
                x.sc == null ? null : x.sc.Name,
                x.ps == null ? null : x.ps.PlannedStartDate,
                x.ps == null ? null : x.ps.PlannedEndDate,
                x.ae == null ? null : x.ae.ActualStartDate,
                x.ae == null ? null : x.ae.ActualEndDate))
            .ToListAsync(cancellationToken);

        DateOnly today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);
        List<TimelineRowResponse> rows = BuildRows(actionItems, columns, today);
        DayOfWeek weekStart = (DayOfWeek)(project.WeekStartDay % 7);

        return new TimelineResponse(
            project.Id,
            scale.ToString(),
            weekStart.ToString(),
            columns,
            rows,
            pageNumber,
            pageSize,
            totalCount,
            totalPages,
            pageNumber > 1,
            pageNumber < totalPages);
    }

    private static List<TimelineRowResponse> BuildRows(
        List<TimelineItemReadModel> actionItems,
        List<TimelineColumnResponse> columns,
        DateOnly today)
    {
        List<TimelineRowResponse> rows = [];
        Guid? currentCategoryId = null;
        Guid? currentSubCategoryId = null;

        foreach (TimelineItemReadModel item in actionItems)
        {
            if (currentCategoryId != item.CategoryId)
            {
                rows.Add(new TimelineRowResponse(
                    "Category",
                    item.CategoryId,
                    item.CategoryName,
                    item.CategoryColor,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null));

                currentCategoryId = item.CategoryId;
                currentSubCategoryId = null;
            }

            if (item.SubCategoryId.HasValue && currentSubCategoryId != item.SubCategoryId)
            {
                rows.Add(new TimelineRowResponse(
                    "SubCategory",
                    item.SubCategoryId.Value,
                    item.SubCategoryName!,
                    null,
                    item.CategoryId,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null,
                    null));

                currentSubCategoryId = item.SubCategoryId;
            }

            rows.Add(MapActionItemRow(item, columns, today));
        }

        return rows;
    }

    private static TimelineRowResponse MapActionItemRow(
        TimelineItemReadModel item,
        List<TimelineColumnResponse> columns,
        DateOnly today)
    {
        ActionItemStatus status = ActionItemStatusService.ComputeStatus(
            item.PlannedEndDate,
            item.ActualStartDate,
            item.ActualEndDate,
            today);

        return new TimelineRowResponse(
            "ActionItem",
            item.Id,
            item.ActionItemName,
            null,
            item.CategoryId,
            item.SubCategoryId,
            GetColumnIndex(item.PlannedStartDate, columns),
            GetColumnIndex(item.PlannedEndDate, columns),
            GetColumnIndex(item.ActualStartDate, columns),
            GetColumnIndex(item.ActualEndDate, columns),
            (int)status,
            status.ToString());
    }

    private static int? GetColumnIndex(
        DateOnly? date,
        List<TimelineColumnResponse> columns)
    {
        if (!date.HasValue || columns.Count == 0)
        {
            return null;
        }

        DateOnly value = date.Value;
        if (value < columns[0].StartDate)
        {
            return 0;
        }

        if (value > columns[^1].EndDate)
        {
            return columns.Count - 1;
        }

        int low = 0;
        int high = columns.Count - 1;

        while (low <= high)
        {
            int middle = low + ((high - low) / 2);
            TimelineColumnResponse column = columns[middle];

            if (value < column.StartDate)
            {
                high = middle - 1;
            }
            else if (value > column.EndDate)
            {
                low = middle + 1;
            }
            else
            {
                return middle;
            }
        }

        return null;
    }

    private static List<TimelineColumnResponse> GenerateColumns(
        TimelineScale scale,
        DateOnly startDate,
        DateOnly endDate,
        int weekStartDay)
    {
        List<TimelineColumnResponse> columns = [];

        switch (scale)
        {
            case TimelineScale.Daily:
                for (DateOnly current = startDate; current <= endDate; current = current.AddDays(1))
                {
                    columns.Add(new TimelineColumnResponse(
                        current.ToString("yyyy-MM-dd"),
                        current,
                        current));
                }
                break;

            case TimelineScale.Weekly:
                {
                    DayOfWeek targetStartDay = (DayOfWeek)(weekStartDay % 7);
                    DateOnly currentStart = AlignToWeekStart(startDate, targetStartDay);
                    int weekNumber = 1;

                    while (currentStart <= endDate)
                    {
                        DateOnly currentEnd = currentStart.AddDays(6);
                        columns.Add(new TimelineColumnResponse(
                            $"WW{weekNumber:D2}",
                            currentStart,
                            currentEnd));
                        currentStart = currentStart.AddDays(7);
                        weekNumber++;
                    }
                }
                break;

            case TimelineScale.Biweekly:
                {
                    DayOfWeek targetStartDay = (DayOfWeek)(weekStartDay % 7);
                    DateOnly currentStart = AlignToWeekStart(startDate, targetStartDay);
                    int biweeklyNumber = 1;

                    while (currentStart <= endDate)
                    {
                        DateOnly currentEnd = currentStart.AddDays(13);
                        columns.Add(new TimelineColumnResponse(
                            $"BW{biweeklyNumber:D2}",
                            currentStart,
                            currentEnd));
                        currentStart = currentStart.AddDays(14);
                        biweeklyNumber++;
                    }
                }
                break;

            case TimelineScale.Monthly:
                {
                    DateOnly currentStart = new(startDate.Year, startDate.Month, 1);
                    while (currentStart <= endDate)
                    {
                        DateOnly currentEnd = currentStart.AddMonths(1).AddDays(-1);
                        columns.Add(new TimelineColumnResponse(
                            currentStart.ToString("MMM yyyy"),
                            currentStart,
                            currentEnd));
                        currentStart = currentStart.AddMonths(1);
                    }
                }
                break;

            case TimelineScale.Quarterly:
                {
                    int firstMonthOfQuarter = ((startDate.Month - 1) / 3) * 3 + 1;
                    DateOnly currentStart = new(startDate.Year, firstMonthOfQuarter, 1);
                    while (currentStart <= endDate)
                    {
                        DateOnly currentEnd = currentStart.AddMonths(3).AddDays(-1);
                        int quarterNumber = ((currentStart.Month - 1) / 3) + 1;
                        columns.Add(new TimelineColumnResponse(
                            $"Q{quarterNumber} {currentStart.Year}",
                            currentStart,
                            currentEnd));
                        currentStart = currentStart.AddMonths(3);
                    }
                }
                break;
        }

        return columns;
    }

    private static DateOnly AlignToWeekStart(DateOnly date, DayOfWeek weekStartDay)
    {
        int difference = (7 + (date.DayOfWeek - weekStartDay)) % 7;
        return date.AddDays(-difference);
    }

    private sealed record TimelineItemReadModel(
        Guid Id,
        string ActionItemName,
        Guid CategoryId,
        string CategoryName,
        string? CategoryColor,
        Guid? SubCategoryId,
        string? SubCategoryName,
        DateOnly? PlannedStartDate,
        DateOnly? PlannedEndDate,
        DateOnly? ActualStartDate,
        DateOnly? ActualEndDate);
}
