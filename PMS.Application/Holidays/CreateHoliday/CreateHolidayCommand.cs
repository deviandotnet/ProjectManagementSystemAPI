using PMS.Application.Abstractions.Messaging;
using PMS.Domain.HolidayCalendars;

namespace PMS.Application.Holidays.CreateHoliday;

public sealed record CreateHolidayCommand(
    DateOnly HolidayDate,
    string Name,
    HolidayType Type,
    bool IsRecurringAnnually = false,
    int? Year = null
) : ICommand<Guid>;
