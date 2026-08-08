using PMS.Application.Abstractions.Messaging;

namespace PMS.Application.Categories.GetCategoriesByProjectId;

public sealed record GetCategoriesByProjectIdQuery(
    Guid ProjectId
) : IQuery<IReadOnlyCollection<CategoryResponse>>;
