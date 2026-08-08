using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using PMS.Domain.Users;

namespace PMS.Infrastructure.Authentication
{
    internal static class ClaimsPrincipalExtensions
    {
        public static Guid? GetUserId(this ClaimsPrincipal? principal)
        {
            if (principal is null)
            {
                return null;
            }

            string? userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? principal.FindFirstValue("sub");

            return Guid.TryParse(userId, out Guid parsedUserId)
                ? parsedUserId
                : null;
        }

        public static SystemRole? GetSystemRole(this ClaimsPrincipal? principal)
        {
            if (principal is null)
            {
                return null;
            }

            string? roleStr = principal.FindFirstValue("system_role")
                ?? principal.FindFirstValue(ClaimTypes.Role)
                ?? principal.FindFirstValue("role");

            return Enum.TryParse<SystemRole>(roleStr, true, out var role)
                ? role
                : null;
        }
    }
}
