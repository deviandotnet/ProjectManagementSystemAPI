using FluentValidation;

namespace PMS.Application.Categories.ReorderCategories;

public sealed class ReorderCategoriesCommandValidator : AbstractValidator<ReorderCategoriesCommand>
{
    public ReorderCategoriesCommandValidator()
    {
        RuleFor(c => c.ProjectId).NotEmpty();
        RuleFor(c => c.Items).NotNull().NotEmpty();
        RuleForEach(c => c.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.CategoryId).NotEmpty();
            item.RuleFor(i => i.DisplayOrder).GreaterThanOrEqualTo(0);
        });
    }
}
