namespace PMS.Application.Projects.GetProjectAuditFeed;

public sealed record AuditFeedItemResponse(
    long Id,
    string EntityName,
    string EntityId,
    string? EntityTitle,
    string Action,
    string? FieldName,
    string? OldValue,
    string? NewValue,
    Guid? ChangedByUserId,
    string? ChangedByName,
    DateTimeOffset ChangedAt,
    string ActivityMessage
);

public sealed record AuditFeedResponse(
    Guid ProjectId,
    string ProjectName,
    IReadOnlyCollection<AuditFeedItemResponse> Feed
);
