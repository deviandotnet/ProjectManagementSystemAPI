using FluentValidation;

namespace PMS.Application.Holidays.DeleteHoliday;

internal sealed class DeleteHolidayCommandValidator : AbstractValidator<DeleteHolidayCommand>
{
    public DeleteHolidayCommandValidator()
    {
        RuleFor(c => c.Id)
            .NotEmpty().WithMessage("Id is required.");
    }
}
