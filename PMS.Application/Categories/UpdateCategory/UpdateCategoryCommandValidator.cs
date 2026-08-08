using FluentValidation;

namespace PMS.Application.Categories.UpdateCategory;

public sealed class UpdateCategoryCommandValidator : AbstractValidator<UpdateCategoryCommand>
{
    public UpdateCategoryCommandValidator()
    {
        RuleFor(c => c.ProjectId)
            .NotEmpty();

        RuleFor(c => c.CategoryId)
            .NotEmpty();

        RuleFor(c => c.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}
