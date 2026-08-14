using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.HolidayCalendars;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.Holidays.UpdateHoliday;

internal sealed class UpdateHolidayCommandHandler(
    IApplicationDbContext context,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<UpdateHolidayCommand>
{
    public async Task<Result> Handle(
        UpdateHolidayCommand command,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure(UserErrors.Unauthorized);
        }

        if (!userContext.IsSystemAdmin)
        {
            return Result.Failure(HolidayErrors.Forbidden);
        }

        HolidayCalendar? holiday = await context.HolidayCalendar
            .FirstOrDefaultAsync(h => h.Id == command.Id, cancellationToken);

        if (holiday is null)
        {
            return Result.Failure(HolidayErrors.NotFound(command.Id));
        }

        int? year = command.IsRecurringAnnually ? null : (command.Year ?? command.HolidayDate.Year);

        holiday.HolidayDate = command.HolidayDate;
        holiday.Name = command.Name;
        holiday.Type = command.Type;
        holiday.IsRecurringAnnually = command.IsRecurringAnnually;
        holiday.Year = year;

        await context.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
