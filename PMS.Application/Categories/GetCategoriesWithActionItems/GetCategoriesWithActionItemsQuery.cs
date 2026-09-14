using PMS.Application.Abstractions.Messaging;

namespace PMS.Application.Categories.GetCategoriesWithActionItems;

public sealed record GetCategoriesWithActionItemsQuery(
    Guid ProjectId,
    Guid? CategoryId = null,
    Guid? SubCategoryId = null,
    int[]? Statuses = null,
    int? Priority = null,
    string? OwnerName = null,
    string? Search = null,
    string? WeekStart = null,
    string? WeekEnd = null,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null,
    int PageNumber = 1,
    int PageSize = 20)
    : IQuery<CategoriesWithActionItemsResponse>;
