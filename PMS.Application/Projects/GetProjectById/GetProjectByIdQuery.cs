using PMS.Application.Abstractions.Messaging;

namespace PMS.Application.Projects.GetProjectById;

public sealed record GetProjectByIdQuery(Guid Id) : IQuery<ProjectResponse>;
