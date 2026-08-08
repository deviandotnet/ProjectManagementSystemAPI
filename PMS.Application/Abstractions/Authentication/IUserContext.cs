using PMS.Domain.Users;
using System;

namespace PMS.Application.Abstractions.Authentication
{
    public interface IUserContext
    {
        Guid? UserId { get; }
        bool IsAuthenticated { get; }
        SystemRole? SystemRole { get; }
        bool IsSystemAdmin { get; }
    }
}
