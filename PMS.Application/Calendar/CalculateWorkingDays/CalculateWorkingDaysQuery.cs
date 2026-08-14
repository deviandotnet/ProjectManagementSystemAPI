using PMS.Application.Abstractions.Messaging;

namespace PMS.Application.Calendar.CalculateWorkingDays;

public sealed record CalculateWorkingDaysQuery(
    Guid ProjectId,
    DateOnly StartDate,
    DateOnly EndDate
) : IQuery<WorkingDaysResponse>;
