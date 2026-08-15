using PMS.Application.Abstractions.Messaging;

namespace PMS.Application.Projects.GetProjectAuditFeed;

public sealed record GetProjectAuditFeedQuery(Guid ProjectId)
    : IQuery<AuditFeedResponse>;
