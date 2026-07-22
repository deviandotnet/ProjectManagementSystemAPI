using FluentValidation;

namespace PMS.Application.Features.ProjectFeatures.CreateProject;

/// <summary>
/// Validator for CreateProjectRequest.
/// Automatically executed by the ValidationDecorator pipeline before reaching the handler.
/// </summary>
public sealed class CreateProjectValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Project name is required.")
            .MaximumLength(200)
            .WithMessage("Project name must not exceed 200 characters.");

        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("End date must be on or after the start date.");

        RuleFor(x => x.WeekStartDay)
            .InclusiveBetween(0, 6)
            .WithMessage("Week start day must be between 0 (Sunday) and 6 (Saturday).");
    }
}
