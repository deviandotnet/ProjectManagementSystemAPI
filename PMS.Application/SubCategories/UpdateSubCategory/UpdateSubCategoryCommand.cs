using PMS.Application.Abstractions.Messaging;

namespace PMS.Application.SubCategories.UpdateSubCategory;

public sealed record UpdateSubCategoryCommand(
    Guid CategoryId,
    Guid SubCategoryId,
    string Name,
    int DisplayOrder
) : ICommand;
