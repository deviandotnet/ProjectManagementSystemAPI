using FluentValidation;

namespace PMS.Application.ActionItems.ReorderActionItems;

public sealed class ReorderActionItemsCommandValidator : AbstractValidator<ReorderActionItemsCommand>
{
    public ReorderActionItemsCommandValidator()
    {
        RuleFor(c => c.ProjectId).NotEmpty();
        RuleFor(c => c.Items).NotNull().NotEmpty();
        RuleForEach(c => c.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ActionItemId).NotEmpty();
            item.RuleFor(i => i.Sequence).GreaterThanOrEqualTo(0);
        });
    }
}
