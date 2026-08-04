using FluentValidation;
using PMS.Domain.Projects;

namespace PMS.Application.Projects.UpdateProject;

public sealed class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.WeekStartDay)
            .InclusiveBetween(0, 6);

        RuleFor(x => x)
            .Must(x => x.EndDate >= x.StartDate)
            .WithMessage("Project EndDate must be on or after StartDate.")
            .WithErrorCode(ProjectErrors.InvalidDates.Code);
    }
}
