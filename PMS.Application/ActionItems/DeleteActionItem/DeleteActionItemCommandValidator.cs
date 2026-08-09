using FluentValidation;

namespace PMS.Application.ActionItems.DeleteActionItem;

internal sealed class DeleteActionItemCommandValidator : AbstractValidator<DeleteActionItemCommand>
{
    public DeleteActionItemCommandValidator()
    {
        RuleFor(c => c.ProjectId)
            .NotEmpty().WithMessage("ProjectId is required.");

        RuleFor(c => c.ActionItemId)
            .NotEmpty().WithMessage("ActionItemId is required.");
    }
}
