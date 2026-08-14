namespace PMS.Application.Calendar.CalculateWorkingDays;

public sealed record HolidayItemDto(
    DateOnly Date,
    string Name,
    string Type
);

public sealed record WorkingDaysResponse(
    Guid ProjectId,
    DateOnly StartDate,
    DateOnly EndDate,
    int CalendarDays,
    int WorkingDays,
    int WeekendDays,
    int HolidayDays,
    List<HolidayItemDto> Holidays
);
