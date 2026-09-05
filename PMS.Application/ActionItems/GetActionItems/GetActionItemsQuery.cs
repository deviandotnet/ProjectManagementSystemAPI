using PMS.Application.Abstractions.Messaging;
using PMS.Application.Abstractions;

namespace PMS.Application.ActionItems.GetActionItems;

public sealed record GetActionItemsQuery(
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
    int PageSize = 20
) : IQuery<PagedResponse<ActionItemResponse>>;
