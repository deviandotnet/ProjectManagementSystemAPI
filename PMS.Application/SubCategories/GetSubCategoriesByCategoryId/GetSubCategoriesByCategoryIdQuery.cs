using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;

namespace PMS.Application.SubCategories.GetSubCategoriesByCategoryId;

public sealed record GetSubCategoriesByCategoryIdQuery(
    Guid CategoryId,
    int PageNumber = 1,
    int PageSize = 20)
    : IQuery<PagedResponse<SubCategoryResponse>>;
