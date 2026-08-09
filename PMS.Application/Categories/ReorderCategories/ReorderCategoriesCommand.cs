using PMS.Application.Abstractions.Messaging;

namespace PMS.Application.Categories.ReorderCategories;

public sealed record ReorderCategoriesCommand(
    Guid ProjectId,
    List<ReorderCategoryItem> Items
) : ICommand;
