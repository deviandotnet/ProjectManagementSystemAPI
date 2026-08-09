using PMS.Application.Abstractions.Messaging;

namespace PMS.Application.ActionItems.DeleteActionItem;

public sealed record DeleteActionItemCommand(
    Guid ProjectId,
    Guid ActionItemId
) : ICommand;
