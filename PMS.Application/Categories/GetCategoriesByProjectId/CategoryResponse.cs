namespace PMS.Application.Categories.GetCategoriesByProjectId;

public sealed record CategoryResponse(
    Guid Id,
    Guid ProjectId,
    string Name,
    int DisplayOrder,
    string? Color,
    Guid? CreatedByUserId
);
