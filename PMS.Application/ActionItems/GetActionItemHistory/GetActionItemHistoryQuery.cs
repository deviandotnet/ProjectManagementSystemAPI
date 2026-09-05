using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;

namespace PMS.Application.ActionItems.GetActionItemHistory;

public sealed record GetActionItemHistoryQuery(
    Guid ProjectId,
    Guid ActionItemId,
    int PageNumber = 1,
    int PageSize = 20
) : IQuery<PagedResponse<ActionItemHistoryResponse>>;
