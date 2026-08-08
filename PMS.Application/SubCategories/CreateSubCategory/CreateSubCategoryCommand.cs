using PMS.Application.Abstractions.Messaging;

namespace PMS.Application.SubCategories.CreateSubCategory;

public sealed record CreateSubCategoryCommand(
    Guid CategoryId,
    string Name,
    int DisplayOrder = 0
) : ICommand<Guid>;
