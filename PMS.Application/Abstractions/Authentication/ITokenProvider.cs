using PMS.Domain.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace PMS.Application.Abstractions.Authentication
{
    public interface ITokenProvider
    {
        string CreateAccessToken(User user);
        string CreateRefreshToken();
    }
}