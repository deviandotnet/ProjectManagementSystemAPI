using PMS.Application.Abstractions.Messaging;

namespace PMS.Application.Projects.GetProjectsByUserId;

public sealed record GetProjectsByUserIdQuery(Guid UserId) : IQuery<List<ProjectResponse>>;
