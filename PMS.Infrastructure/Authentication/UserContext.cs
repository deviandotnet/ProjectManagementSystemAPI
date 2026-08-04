using Microsoft.AspNetCore.Http;
using PMS.Application.Abstractions.Authentication;
using System;

namespace PMS.Infrastructure.Authentication
{
    public class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
    {
        public Guid? UserId => httpContextAccessor.HttpContext?.User.GetUserId() ?? null;

        public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
    }
}
