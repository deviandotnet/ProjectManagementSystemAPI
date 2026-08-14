using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.HolidayCalendars;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.Calendar.CalculateWorkingDays;

internal sealed class CalculateWorkingDaysQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : IQueryHandler<CalculateWorkingDaysQuery, WorkingDaysResponse>
{
    public async Task<Result<WorkingDaysResponse>> Handle(
        CalculateWorkingDaysQuery query,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure<WorkingDaysResponse>(UserErrors.Unauthorized);
        }

        Guid userId = userContext.UserId.Value;

        Project? project = await context.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == query.ProjectId, cancellationToken);

        if (project is null)
        {
            return Result.Failure<WorkingDaysResponse>(ProjectErrors.NotFound(query.ProjectId));
        }

        if (!userContext.IsSystemAdmin)
        {
            bool isMember = await context.ProjectMembers
                .AsNoTracking()
                .AnyAsync(pm => pm.ProjectId == query.ProjectId && pm.UserId == userId, cancellationToken);

            if (!isMember)
            {
                return Result.Failure<WorkingDaysResponse>(ProjectErrors.Forbidden);
            }
        }

        if (query.EndDate < query.StartDate)
        {
            return Result.Failure<WorkingDaysResponse>(HolidayErrors.InvalidDateRange);
        }

        List<HolidayCalendar> holidays = await context.HolidayCalendar
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        int totalCalendarDays = (query.EndDate.DayNumber - query.StartDate.DayNumber) + 1;
        int weekendDays = 0;
        int holidayDays = 0;
        int workingDays = 0;

        List<HolidayItemDto> matchingHolidays = [];

        for (DateOnly date = query.StartDate; date <= query.EndDate; date = date.AddDays(1))
        {
            bool isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

            HolidayCalendar? holiday = holidays.FirstOrDefault(h =>
                (h.IsRecurringAnnually && h.HolidayDate.Month == date.Month && h.HolidayDate.Day == date.Day) ||
                (!h.IsRecurringAnnually && h.HolidayDate == date));

            if (isWeekend)
            {
                weekendDays++;
                if (holiday is not null)
                {
                    matchingHolidays.Add(new HolidayItemDto(
                        date,
                        $"{holiday.Name} (Falls on Weekend)",
                        holiday.Type.ToString()));
                }
            }
            else if (holiday is not null)
            {
                holidayDays++;
                matchingHolidays.Add(new HolidayItemDto(
                    date,
                    holiday.Name,
                    holiday.Type.ToString()));
            }
            else
            {
                workingDays++;
            }
        }

        return new WorkingDaysResponse(
            ProjectId: query.ProjectId,
            StartDate: query.StartDate,
            EndDate: query.EndDate,
            CalendarDays: totalCalendarDays,
            WorkingDays: workingDays,
            WeekendDays: weekendDays,
            HolidayDays: holidayDays,
            Holidays: matchingHolidays);
    }
}
