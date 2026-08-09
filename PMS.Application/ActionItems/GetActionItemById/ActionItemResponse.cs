namespace PMS.Application.ActionItems.GetActionItemById;

public sealed record ActionItemResponse(
    Guid Id,
    string ActionItemName,
    Guid CategoryId,
    string CategoryName,
    Guid? SubCategoryId,
    string? SubCategoryName,
    int Priority,
    string? OwnerName,
    int Sequence,
    PlannedScheduleResponse? PlannedSchedule,
    ActualExecutionResponse? ActualExecution,
    int ComputedStatus,
    string ComputedStatusLabel,
    decimal? Weight,
    string? Remarks
);

public sealed record PlannedScheduleResponse(
    Guid Id,
    DateOnly PlannedStartDate,
    DateOnly PlannedEndDate,
    string PlannedStartWeek,
    string PlannedEndWeek,
    int DurationCalendarDays,
    int DurationWorkingDays
);

public sealed record ActualExecutionResponse(
    Guid Id,
    DateOnly? ActualStartDate,
    DateOnly? ActualEndDate,
    decimal? ActualHours,
    string? DelayReason
);
