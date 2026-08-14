using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.HolidayCalendars;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.Holidays.GetHolidays;

internal sealed class GetHolidaysQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : IQueryHandler<GetHolidaysQuery, IReadOnlyCollection<HolidayResponse>>
{
    public async Task<Result<IReadOnlyCollection<HolidayResponse>>> Handle(
        GetHolidaysQuery query,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure<IReadOnlyCollection<HolidayResponse>>(UserErrors.Unauthorized);
        }

        var dbQuery = context.HolidayCalendar.AsNoTracking();

        if (query.Type.HasValue)
        {
            dbQuery = dbQuery.Where(h => h.Type == query.Type.Value);
        }

        if (query.Year.HasValue)
        {
            int year = query.Year.Value;
            dbQuery = dbQuery.Where(h => h.IsRecurringAnnually || h.Year == year || (h.Year == null && h.HolidayDate.Year == year));
        }

        List<HolidayResponse> holidays = await dbQuery
            .OrderBy(h => h.HolidayDate)
            .Select(h => new HolidayResponse(
                h.Id,
                h.HolidayDate,
                h.Name,
                (int)h.Type,
                h.Type.ToString(),
                h.IsRecurringAnnually,
                h.Year,
                h.CreatedAt))
            .ToListAsync(cancellationToken);

        return holidays;
    }
}
