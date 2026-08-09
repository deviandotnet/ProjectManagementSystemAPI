using PMS.Application.Abstractions.Messaging;
using PMS.Domain.ActionItems;

namespace PMS.Application.ActionItems.UpdateActionItem;

public sealed record UpdateActionItemCommand(
    Guid ProjectId,
    Guid ActionItemId,
    Guid CategoryId,
    Guid? SubCategoryId,
    string ActionItemName,
    string? Description,
    Priority Priority,
    string? OwnerName,
    Guid? OwnerId,
    decimal? Weight,
    int Sequence,
    string? Remarks,
    DateOnly PlannedStartDate,
    DateOnly PlannedEndDate,
    DateOnly? ActualStartDate = null,
    DateOnly? ActualEndDate = null,
    decimal? ActualHours = null,
    string? DelayReason = null
) : ICommand;
