using PMS.Application.Abstractions.Messaging;
using PMS.Domain.HolidayCalendars;

namespace PMS.Application.Holidays.UpdateHoliday;

public sealed record UpdateHolidayCommand(
    Guid Id,
    DateOnly HolidayDate,
    string Name,
    HolidayType Type,
    bool IsRecurringAnnually = false,
    int? Year = null
) : ICommand;
