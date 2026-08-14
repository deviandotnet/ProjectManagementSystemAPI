using PMS.SharedKernel;

namespace PMS.Domain.HolidayCalendars;

public static class HolidayErrors
{
    public static Error NotFound(Guid holidayId) => Error.NotFound(
        "Holidays.NotFound",
        $"The holiday with Id '{holidayId}' was not found.");

    public static readonly Error AlreadyExists = Error.Conflict(
        "Holidays.AlreadyExists",
        "A holiday on this date already exists for the specified year.");

    public static readonly Error InvalidDateRange = Error.Problem(
        "Calendar.InvalidDateRange",
        "EndDate must be on or after StartDate.");

    public static readonly Error Forbidden = Error.Failure(
        "Holidays.Forbidden",
        "Only system administrators can manage the holiday calendar.");
}
