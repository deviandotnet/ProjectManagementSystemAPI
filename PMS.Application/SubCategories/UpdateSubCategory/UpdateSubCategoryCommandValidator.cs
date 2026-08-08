using FluentValidation;

namespace PMS.Application.SubCategories.UpdateSubCategory;

internal sealed class UpdateSubCategoryCommandValidator : AbstractValidator<UpdateSubCategoryCommand>
{
    public UpdateSubCategoryCommandValidator()
    {
        RuleFor(c => c.CategoryId)
            .NotEmpty().WithMessage("CategoryId is required.");

        RuleFor(c => c.SubCategoryId)
            .NotEmpty().WithMessage("SubCategoryId is required.");

        RuleFor(c => c.Name)
            .NotEmpty().WithMessage("SubCategory name is required.")
            .MaximumLength(150).WithMessage("SubCategory name must not exceed 150 characters.");
    }
}
