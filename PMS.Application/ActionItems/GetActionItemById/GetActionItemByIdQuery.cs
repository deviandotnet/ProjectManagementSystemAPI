using PMS.Application.Abstractions.Messaging;

namespace PMS.Application.ActionItems.GetActionItemById;

public sealed record GetActionItemByIdQuery(
    Guid ProjectId,
    Guid ActionItemId
) : IQuery<ActionItemResponse>;
