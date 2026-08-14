namespace PMS.Application.Projects.GetTimeline;

public sealed record TimelineColumnResponse(
    string Label,
    DateOnly StartDate,
    DateOnly EndDate
);

public sealed record TimelineRowResponse(
    string RowType,
    Guid Id,
    string Label,
    string? Color,
    Guid? CategoryId,
    Guid? SubCategoryId,
    int? PlannedStartWeekIndex,
    int? PlannedEndWeekIndex,
    int? ActualStartWeekIndex,
    int? ActualEndWeekIndex,
    int? Status,
    string? StatusLabel
);

public sealed record TimelineResponse(
    Guid ProjectId,
    string Scale,
    string WeekStartDay,
    List<TimelineColumnResponse> Columns,
    List<TimelineRowResponse> Rows
);
