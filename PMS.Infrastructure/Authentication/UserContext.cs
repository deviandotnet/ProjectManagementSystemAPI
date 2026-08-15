using Microsoft.AspNetCore.Http;
using PMS.Application.Abstractions.Authentication;
using PMS.Domain.Users;
using System;

namespace PMS.Infrastructure.Authentication
{
    public class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
    {
        public Guid? UserId => httpContextAccessor.HttpContext?.User.GetUserId();

        public string? Email => httpContextAccessor.HttpContext?.User.GetEmail();

        public string? Name => httpContextAccessor.HttpContext?.User.GetName();

        public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

        public SystemRole? SystemRole => httpContextAccessor.HttpContext?.User.GetSystemRole();

        public bool IsSystemAdmin => SystemRole == PMS.Domain.Users.SystemRole.Admin;
    }
}
