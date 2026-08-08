using PMS.Application.Abstractions.Messaging;

namespace PMS.Application.Categories.UpdateCategory;

public sealed record UpdateCategoryCommand(
    Guid ProjectId,
    Guid CategoryId,
    string Name,
    int DisplayOrder,
    string? Color
) : ICommand;
