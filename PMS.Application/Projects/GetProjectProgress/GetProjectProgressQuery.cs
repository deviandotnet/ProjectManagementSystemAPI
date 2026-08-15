using PMS.Application.Abstractions.Messaging;

namespace PMS.Application.Projects.GetProjectProgress;

public sealed record GetProjectProgressQuery(Guid ProjectId)
    : IQuery<ProjectProgressResponse>;
