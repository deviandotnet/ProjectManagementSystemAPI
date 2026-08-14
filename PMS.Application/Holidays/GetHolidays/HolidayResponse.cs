namespace PMS.Application.Holidays.GetHolidays;

public sealed record HolidayResponse(
    Guid Id,
    DateOnly HolidayDate,
    string Name,
    int Type,
    string TypeLabel,
    bool IsRecurringAnnually,
    int? Year,
    DateTimeOffset CreatedAt
);
