using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.ActionItems;
using PMS.Domain.ActualExecutions;
using PMS.Domain.Categories;
using PMS.Domain.PlannedSchedules;
using PMS.Domain.Projects;
using PMS.Domain.SubCategories;
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

        List<TimelineColumnResponse> columns = GenerateColumns(scale, startDate, endDate, project.WeekStartDay);

        List<Category> categories = await context.Categories
            .AsNoTracking()
            .Where(c => c.ProjectId == query.ProjectId)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(cancellationToken);

        List<SubCategory> subCategories = await context.SubCategories
            .AsNoTracking()
            .Where(sc => categories.Select(c => c.Id).Contains(sc.CategoryId))
            .OrderBy(sc => sc.DisplayOrder)
            .ToListAsync(cancellationToken);

        var actionItemsRaw = await (from ai in context.ActionItems.AsNoTracking()
                                    join ps in context.PlannedSchedules.AsNoTracking() on ai.Id equals ps.ActionItemId into psGroup
                                    from ps in psGroup.DefaultIfEmpty()
                                    join ae in context.ActualExecutions.AsNoTracking() on ai.Id equals ae.ActionItemId into aeGroup
                                    from ae in aeGroup.DefaultIfEmpty()
                                    where ai.ProjectId == query.ProjectId
                                    select new { ai, ps, ae })
                                    .OrderBy(x => x.ai.Sequence)
                                    .ToListAsync(cancellationToken);

        DateOnly today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);
        DayOfWeek weekStartEnum = (DayOfWeek)(project.WeekStartDay % 7);

        List<TimelineRowResponse> rows = [];

        foreach (Category category in categories)
        {
            rows.Add(new TimelineRowResponse(
                RowType: "Category",
                Id: category.Id,
                Label: category.Name,
                Color: category.Color,
                CategoryId: null,
                SubCategoryId: null,
                PlannedStartWeekIndex: null,
                PlannedEndWeekIndex: null,
                ActualStartWeekIndex: null,
                ActualEndWeekIndex: null,
                Status: null,
                StatusLabel: null
            ));

            var directItems = actionItemsRaw.Where(x => x.ai.CategoryId == category.Id && x.ai.SubCategoryId == null);
            foreach (var item in directItems)
            {
                rows.Add(MapActionItemRow(item.ai, item.ps, item.ae, columns, today));
            }

            var categorySubCats = subCategories.Where(sc => sc.CategoryId == category.Id);
            foreach (SubCategory subCat in categorySubCats)
            {
                rows.Add(new TimelineRowResponse(
                    RowType: "SubCategory",
                    Id: subCat.Id,
                    Label: subCat.Name,
                    Color: null,
                    CategoryId: category.Id,
                    SubCategoryId: null,
                    PlannedStartWeekIndex: null,
                    PlannedEndWeekIndex: null,
                    ActualStartWeekIndex: null,
                    ActualEndWeekIndex: null,
                    Status: null,
                    StatusLabel: null
                ));

                var subCatItems = actionItemsRaw.Where(x => x.ai.SubCategoryId == subCat.Id);
                foreach (var item in subCatItems)
                {
                    rows.Add(MapActionItemRow(item.ai, item.ps, item.ae, columns, today));
                }
            }
        }

        return new TimelineResponse(
            ProjectId: project.Id,
            Scale: scale.ToString(),
            WeekStartDay: weekStartEnum.ToString(),
            Columns: columns,
            Rows: rows
        );
    }

    private static TimelineRowResponse MapActionItemRow(
        ActionItem ai,
        PlannedSchedule? ps,
        ActualExecution? ae,
        List<TimelineColumnResponse> columns,
        DateOnly today)
    {
        ActionItemStatus status = ComputeStatus(ps, ae, today);

        int? plannedStartIndex = GetColumnIndex(ps?.PlannedStartDate, columns);
        int? plannedEndIndex = GetColumnIndex(ps?.PlannedEndDate, columns);
        int? actualStartIndex = GetColumnIndex(ae?.ActualStartDate, columns);
        int? actualEndIndex = GetColumnIndex(ae?.ActualEndDate, columns);

        return new TimelineRowResponse(
            RowType: "ActionItem",
            Id: ai.Id,
            Label: ai.ActionItemName,
            Color: null,
            CategoryId: ai.CategoryId,
            SubCategoryId: ai.SubCategoryId,
            PlannedStartWeekIndex: plannedStartIndex,
            PlannedEndWeekIndex: plannedEndIndex,
            ActualStartWeekIndex: actualStartIndex,
            ActualEndWeekIndex: actualEndIndex,
            Status: (int)status,
            StatusLabel: status.ToString()
        );
    }

    private static int? GetColumnIndex(DateOnly? date, List<TimelineColumnResponse> columns)
    {
        if (!date.HasValue || columns.Count == 0)
        {
            return null;
        }

        DateOnly d = date.Value;
        if (d < columns[0].StartDate) return 0;
        if (d > columns[^1].EndDate) return columns.Count - 1;

        for (int i = 0; i < columns.Count; i++)
        {
            if (d >= columns[i].StartDate && d <= columns[i].EndDate)
            {
                return i;
            }
        }

        return null;
    }

    private static ActionItemStatus ComputeStatus(
        PlannedSchedule? planned,
        ActualExecution? actual,
        DateOnly today)
    {
        if (planned is null) return ActionItemStatus.Plan;

        if (actual?.ActualEndDate is not null)
        {
            if (actual.ActualEndDate < planned.PlannedEndDate)
                return ActionItemStatus.CompletedEarly;
            if (actual.ActualEndDate == planned.PlannedEndDate)
                return ActionItemStatus.CompletedOntime;
            return ActionItemStatus.CompletedLate;
        }

        if (actual?.ActualStartDate is not null)
            return ActionItemStatus.Ongoing;

        if (today > planned.PlannedEndDate)
            return ActionItemStatus.Delayed;

        return ActionItemStatus.Plan;
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
                for (DateOnly curr = startDate; curr <= endDate; curr = curr.AddDays(1))
                {
                    columns.Add(new TimelineColumnResponse(
                        Label: curr.ToString("yyyy-MM-dd"),
                        StartDate: curr,
                        EndDate: curr));
                }
                break;

            case TimelineScale.Weekly:
                {
                    DayOfWeek targetStartDay = (DayOfWeek)(weekStartDay % 7);
                    DateOnly currStart = AlignToWeekStart(startDate, targetStartDay);
                    int weekNum = 1;

                    while (currStart <= endDate)
                    {
                        DateOnly currEnd = currStart.AddDays(6);
                        columns.Add(new TimelineColumnResponse(
                            Label: $"WW{weekNum:D2}",
                            StartDate: currStart,
                            EndDate: currEnd));
                        currStart = currStart.AddDays(7);
                        weekNum++;
                    }
                }
                break;

            case TimelineScale.Biweekly:
                {
                    DayOfWeek targetStartDay = (DayOfWeek)(weekStartDay % 7);
                    DateOnly currStart = AlignToWeekStart(startDate, targetStartDay);
                    int bwNum = 1;

                    while (currStart <= endDate)
                    {
                        DateOnly currEnd = currStart.AddDays(13);
                        columns.Add(new TimelineColumnResponse(
                            Label: $"BW{bwNum:D2}",
                            StartDate: currStart,
                            EndDate: currEnd));
                        currStart = currStart.AddDays(14);
                        bwNum++;
                    }
                }
                break;

            case TimelineScale.Monthly:
                {
                    DateOnly currStart = new DateOnly(startDate.Year, startDate.Month, 1);
                    while (currStart <= endDate)
                    {
                        DateOnly currEnd = currStart.AddMonths(1).AddDays(-1);
                        columns.Add(new TimelineColumnResponse(
                            Label: currStart.ToString("MMM yyyy"),
                            StartDate: currStart,
                            EndDate: currEnd));
                        currStart = currStart.AddMonths(1);
                    }
                }
                break;

            case TimelineScale.Quarterly:
                {
                    int firstMonthOfQuarter = ((startDate.Month - 1) / 3) * 3 + 1;
                    DateOnly currStart = new DateOnly(startDate.Year, firstMonthOfQuarter, 1);
                    while (currStart <= endDate)
                    {
                        DateOnly currEnd = currStart.AddMonths(3).AddDays(-1);
                        int quarterNum = ((currStart.Month - 1) / 3) + 1;
                        columns.Add(new TimelineColumnResponse(
                            Label: $"Q{quarterNum} {currStart.Year}",
                            StartDate: currStart,
                            EndDate: currEnd));
                        currStart = currStart.AddMonths(3);
                    }
                }
                break;
        }

        return columns;
    }

    private static DateOnly AlignToWeekStart(DateOnly date, DayOfWeek weekStartDay)
    {
        int diff = (7 + (date.DayOfWeek - weekStartDay)) % 7;
        return date.AddDays(-diff);
    }
}
