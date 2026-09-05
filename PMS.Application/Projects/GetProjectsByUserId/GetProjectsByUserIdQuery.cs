using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;

namespace PMS.Application.Projects.GetProjectsByUserId;

public sealed record GetProjectsByUserIdQuery(
    Guid UserId,
    int PageNumber = 1,
    int PageSize = 20)
    : IQuery<PagedResponse<ProjectResponse>>;
