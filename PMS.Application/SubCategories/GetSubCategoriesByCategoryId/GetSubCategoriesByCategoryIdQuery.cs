using PMS.Application.Abstractions.Messaging;

namespace PMS.Application.SubCategories.GetSubCategoriesByCategoryId;

public sealed record GetSubCategoriesByCategoryIdQuery(Guid CategoryId)
    : IQuery<IReadOnlyCollection<SubCategoryResponse>>;
