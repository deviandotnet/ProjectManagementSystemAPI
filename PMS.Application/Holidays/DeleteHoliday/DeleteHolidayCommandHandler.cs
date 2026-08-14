using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.HolidayCalendars;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.Holidays.DeleteHoliday;

internal sealed class DeleteHolidayCommandHandler(
    IApplicationDbContext context,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<DeleteHolidayCommand>
{
    public async Task<Result> Handle(
        DeleteHolidayCommand command,
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

        context.HolidayCalendar.Remove(holiday);

        await context.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
