using FluentValidation;

namespace PMS.Application.Holidays.CreateHoliday;

internal sealed class CreateHolidayCommandValidator : AbstractValidator<CreateHolidayCommand>
{
    public CreateHolidayCommandValidator()
    {
        RuleFor(c => c.HolidayDate)
            .NotEmpty().WithMessage("HolidayDate is required.");

        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(c => c.Type)
            .IsInEnum().WithMessage("Invalid holiday type.");
    }
}
