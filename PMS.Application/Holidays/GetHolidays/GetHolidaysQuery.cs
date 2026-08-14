using PMS.Application.Abstractions.Messaging;
using PMS.Domain.HolidayCalendars;

namespace PMS.Application.Holidays.GetHolidays;

public sealed record GetHolidaysQuery(
    int? Year = null,
    HolidayType? Type = null
) : IQuery<IReadOnlyCollection<HolidayResponse>>;
