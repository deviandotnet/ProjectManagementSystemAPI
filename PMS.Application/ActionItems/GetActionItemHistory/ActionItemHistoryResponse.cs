namespace PMS.Application.ActionItems.GetActionItemHistory;

public sealed record ActionItemHistoryResponse(
    long Id,
    string EntityName,
    string EntityId,
    string Action,
    string? FieldName,
    string? OldValue,
    string? NewValue,
    Guid? ChangedByUserId,
    string? ChangedByName,
    DateTimeOffset ChangedAt
);
