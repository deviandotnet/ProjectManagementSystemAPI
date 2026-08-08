namespace PMS.Application.SubCategories.GetSubCategoriesByCategoryId;

public sealed record SubCategoryResponse(
    Guid Id,
    Guid CategoryId,
    string Name,
    int DisplayOrder
);
