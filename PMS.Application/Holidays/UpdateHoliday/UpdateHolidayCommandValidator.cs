using FluentValidation;

namespace PMS.Application.Holidays.UpdateHoliday;

internal sealed class UpdateHolidayCommandValidator : AbstractValidator<UpdateHolidayCommand>
{
    public UpdateHolidayCommandValidator()
    {
        RuleFor(c => c.Id)
            .NotEmpty().WithMessage("Id is required.");

        RuleFor(c => c.HolidayDate)
            .NotEmpty().WithMessage("HolidayDate is required.");

        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(c => c.Type)
            .IsInEnum().WithMessage("Invalid holiday type.");
    }
}
