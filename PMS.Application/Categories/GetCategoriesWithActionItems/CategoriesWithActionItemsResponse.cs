namespace PMS.Application.Categories.GetCategoriesWithActionItems;

public sealed record CategoriesWithActionItemsResponse(
    IReadOnlyCollection<CategoryWithActionItemsResponse> Categories,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);

public sealed record CategoryWithActionItemsResponse(
    Guid Id,
    Guid ProjectId,
    string Name,
    int DisplayOrder,
    string? Color,
    IReadOnlyCollection<CategorizedActionItemResponse> ActionItems);

public sealed record CategorizedActionItemResponse(
    Guid Id,
    string ActionItemName,
    Guid CategoryId,
    string CategoryName,
    Guid? SubCategoryId,
    string? SubCategoryName,
    int Priority,
    string? OwnerName,
    int Sequence,
    CategorizedPlannedScheduleResponse? PlannedSchedule,
    CategorizedActualExecutionResponse? ActualExecution,
    int ComputedStatus,
    string ComputedStatusLabel,
    decimal? Weight,
    string? Remarks);

public sealed record CategorizedPlannedScheduleResponse(
    Guid Id,
    DateOnly PlannedStartDate,
    DateOnly PlannedEndDate,
    string PlannedStartWeek,
    string PlannedEndWeek,
    int DurationCalendarDays,
    int DurationWorkingDays);

public sealed record CategorizedActualExecutionResponse(
    Guid Id,
    DateOnly? ActualStartDate,
    DateOnly? ActualEndDate,
    decimal? ActualHours,
    string? DelayReason);
