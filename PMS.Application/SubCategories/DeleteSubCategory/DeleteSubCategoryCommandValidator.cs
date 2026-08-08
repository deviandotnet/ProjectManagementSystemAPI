using FluentValidation;

namespace PMS.Application.SubCategories.DeleteSubCategory;

internal sealed class DeleteSubCategoryCommandValidator : AbstractValidator<DeleteSubCategoryCommand>
{
    public DeleteSubCategoryCommandValidator()
    {
        RuleFor(c => c.CategoryId)
            .NotEmpty().WithMessage("CategoryId is required.");

        RuleFor(c => c.SubCategoryId)
            .NotEmpty().WithMessage("SubCategoryId is required.");
    }
}
