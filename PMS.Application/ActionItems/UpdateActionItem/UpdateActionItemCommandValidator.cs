using FluentValidation;

namespace PMS.Application.ActionItems.UpdateActionItem;

internal sealed class UpdateActionItemCommandValidator : AbstractValidator<UpdateActionItemCommand>
{
    public UpdateActionItemCommandValidator()
    {
        RuleFor(c => c.ProjectId)
            .NotEmpty().WithMessage("ProjectId is required.");

        RuleFor(c => c.ActionItemId)
            .NotEmpty().WithMessage("ActionItemId is required.");

        RuleFor(c => c.CategoryId)
            .NotEmpty().WithMessage("CategoryId is required.");

        RuleFor(c => c.ActionItemName)
            .NotEmpty().WithMessage("ActionItemName is required.")
            .MaximumLength(500).WithMessage("ActionItemName must not exceed 500 characters.");

        RuleFor(c => c.PlannedStartDate)
            .NotEmpty().WithMessage("PlannedStartDate is required.");

        RuleFor(c => c.PlannedEndDate)
            .NotEmpty().WithMessage("PlannedEndDate is required.")
            .GreaterThanOrEqualTo(c => c.PlannedStartDate)
            .WithMessage("PlannedEndDate must be on or after PlannedStartDate.");

        RuleFor(c => c.Priority)
            .IsInEnum().WithMessage("Invalid priority value.");

        RuleFor(c => c.Weight)
            .InclusiveBetween(0m, 100m)
            .When(c => c.Weight.HasValue)
            .WithMessage("Weight must be between 0 and 100.");

        RuleFor(c => c.ActualEndDate)
            .GreaterThanOrEqualTo(c => c.ActualStartDate!.Value)
            .When(c => c.ActualStartDate.HasValue && c.ActualEndDate.HasValue)
            .WithMessage("ActualEndDate must be on or after ActualStartDate.");
    }
}
