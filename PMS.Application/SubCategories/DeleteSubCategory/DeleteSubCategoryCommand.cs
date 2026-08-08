using PMS.Application.Abstractions.Messaging;

namespace PMS.Application.SubCategories.DeleteSubCategory;

public sealed record DeleteSubCategoryCommand(
    Guid CategoryId,
    Guid SubCategoryId
) : ICommand;
