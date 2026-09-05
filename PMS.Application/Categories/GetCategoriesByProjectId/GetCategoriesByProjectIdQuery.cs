using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;

namespace PMS.Application.Categories.GetCategoriesByProjectId;

public sealed record GetCategoriesByProjectIdQuery(
    Guid ProjectId,
    int PageNumber = 1,
    int PageSize = 20
) : IQuery<PagedResponse<CategoryResponse>>;
