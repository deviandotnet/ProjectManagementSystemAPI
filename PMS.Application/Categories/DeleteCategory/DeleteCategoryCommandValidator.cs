using FluentValidation;

namespace PMS.Application.Categories.DeleteCategory;

internal sealed class DeleteCategoryCommandValidator : AbstractValidator<DeleteCategoryCommand>
{
    public DeleteCategoryCommandValidator()
    {
        RuleFor(c => c.ProjectId)
            .NotEmpty().WithMessage("ProjectId is required.");

        RuleFor(c => c.CategoryId)
            .NotEmpty().WithMessage("CategoryId is required.");
    }
}
