using PMS.Application.Abstractions.Messaging;

namespace PMS.Application.Categories.DeleteCategory;

public sealed record DeleteCategoryCommand(
    Guid ProjectId,
    Guid CategoryId
) : ICommand;
