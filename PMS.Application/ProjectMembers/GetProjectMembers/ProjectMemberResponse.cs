using System;
using PMS.Domain.Users;

namespace PMS.Application.ProjectMembers.GetProjectMembers;

public sealed record ProjectMemberResponse(
    Guid MemberId,
    Guid ProjectId,
    Guid UserId,
    string FirstName,
    string LastName,
    string Email,
    UserRole Role,
    DateTimeOffset JoinedAt
);
