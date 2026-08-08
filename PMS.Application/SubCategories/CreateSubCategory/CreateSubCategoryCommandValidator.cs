using FluentValidation;

namespace PMS.Application.SubCategories.CreateSubCategory;

internal sealed class CreateSubCategoryCommandValidator : AbstractValidator<CreateSubCategoryCommand>
{
    public CreateSubCategoryCommandValidator()
    {
        RuleFor(c => c.CategoryId)
            .NotEmpty().WithMessage("CategoryId is required.");

        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("SubCategory name is required.")
            .MaximumLength(150).WithMessage("SubCategory name must not exceed 150 characters.");
    }
}
