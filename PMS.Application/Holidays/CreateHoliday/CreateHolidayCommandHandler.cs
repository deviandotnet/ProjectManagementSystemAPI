using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.HolidayCalendars;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.Holidays.CreateHoliday;

internal sealed class CreateHolidayCommandHandler(
    IApplicationDbContext context,
    IUnitOfWork unitOfWork,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateHolidayCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateHolidayCommand command,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure<Guid>(UserErrors.Unauthorized);
        }

        if (!userContext.IsSystemAdmin)
        {
            return Result.Failure<Guid>(HolidayErrors.Forbidden);
        }

        int? year = command.IsRecurringAnnually ? null : (command.Year ?? command.HolidayDate.Year);

        bool exists = await context.HolidayCalendar
            .AnyAsync(h => h.HolidayDate == command.HolidayDate && h.Year == year, cancellationToken);

        if (exists)
        {
            return Result.Failure<Guid>(HolidayErrors.AlreadyExists);
        }

        var holiday = new HolidayCalendar
        {
            Id = Guid.NewGuid(),
            HolidayDate = command.HolidayDate,
            Name = command.Name,
            Type = command.Type,
            IsRecurringAnnually = command.IsRecurringAnnually,
            Year = year,
            CreatedAt = dateTimeProvider.UtcNow
        };

        context.HolidayCalendar.Add(holiday);

        await context.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return holiday.Id;
    }
}
