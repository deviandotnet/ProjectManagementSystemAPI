using PMS.Application.Abstractions.Messaging;

namespace PMS.Application.ActionItems.GetActionItemHistory;

public sealed record GetActionItemHistoryQuery(
    Guid ProjectId,
    Guid ActionItemId
) : IQuery<IReadOnlyCollection<ActionItemHistoryResponse>>;
