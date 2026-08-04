using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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
    }
}
