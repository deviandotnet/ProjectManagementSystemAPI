using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Domain.Users;

namespace PMS.Infrastructure.Authorization;

public class ProjectRoleAuthorizationHandler(
    IHttpContextAccessor httpContextAccessor,
    IApplicationDbContext dbContext,
    IUserContext userContext)
    : AuthorizationHandler<ProjectRoleRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ProjectRoleRequirement requirement)
    {
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return;
        }

        // SystemAdmin bypasses all project-level role checks
        if (userContext.IsSystemAdmin)
        {
            context.Succeed(requirement);
            return;
        }

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return;
        }

        RouteData routeData = httpContext.GetRouteData();
        string? projectIdString = routeData.Values["id"]?.ToString()
            ?? routeData.Values["projectId"]?.ToString();

        if (!Guid.TryParse(projectIdString, out Guid projectId))
        {
            return;
        }

        bool hasRole = await dbContext.ProjectMembers
            .AsNoTracking()
            .AnyAsync(m =>
                m.ProjectId == projectId &&
                m.UserId == userContext.UserId.Value &&
                m.Role <= requirement.MinRole);

        if (hasRole)
        {
            context.Succeed(requirement);
        }
    }
}
