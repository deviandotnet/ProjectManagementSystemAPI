using PMS.Application.Abstractions.Messaging;

namespace PMS.Application.Holidays.DeleteHoliday;

public sealed record DeleteHolidayCommand(
    Guid Id
) : ICommand;
