using PMS.Application.Abstractions.Messaging;

namespace PMS.Application.ActionItems.ReorderActionItems;

public sealed record ReorderActionItemsCommand(
    Guid ProjectId,
    List<ReorderActionItemItem> Items
) : ICommand;
