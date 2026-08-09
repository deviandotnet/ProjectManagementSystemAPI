using PMS.Application.Abstractions.Messaging;

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
    DateOnly? EndDate = null
) : IQuery<IReadOnlyCollection<ActionItemResponse>>;
