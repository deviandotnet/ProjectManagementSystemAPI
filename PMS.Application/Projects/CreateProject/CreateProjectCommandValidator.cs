using FluentValidation;

namespace PMS.Application.Projects.CreateProject;

public sealed class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty()
            .WithMessage("Project name is required.")
            .MaximumLength(200)
            .WithMessage("Project name must not exceed 200 characters.");

        RuleFor(c => c.EndDate)
            .GreaterThanOrEqualTo(c => c.StartDate)
            .WithMessage("End date must be on or after the start date.");

        RuleFor(c => c.WeekStartDay)
            .InclusiveBetween(0, 6)
            .WithMessage("Week start day must be between 0 (Sunday) and 6 (Saturday).");

        RuleFor(c => c.DefaultTimelineScale)
            .IsInEnum();

        RuleFor(c => c.ProgressMode)
            .IsInEnum();
    }
}
