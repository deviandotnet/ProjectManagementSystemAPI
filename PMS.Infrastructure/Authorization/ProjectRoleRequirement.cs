using Microsoft.AspNetCore.Authorization;
using PMS.Domain.Users;

namespace PMS.Infrastructure.Authorization;

public class ProjectRoleRequirement(UserRole minRole) : IAuthorizationRequirement
{
    public UserRole MinRole { get; } = minRole;
}
