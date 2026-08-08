using PMS.Application.Abstractions.Messaging;

namespace PMS.Application.Categories.CreateCategory;

public sealed record CreateCategoryCommand(
    Guid ProjectId,
    string Name,
    int DisplayOrder,
    string? Color
) : ICommand<Guid>;
