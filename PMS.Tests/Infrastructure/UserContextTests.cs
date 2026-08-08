using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using PMS.Domain.Users;
using PMS.Infrastructure.Authentication;
using System.Security.Claims;
using Xunit;

namespace PMS.UnitTests.Infrastructure;

public class UserContextTests
{
    private readonly IHttpContextAccessor _httpContextAccessor = Substitute.For<IHttpContextAccessor>();

    [Fact]
    public void UserId_Should_ReturnNull_WhenHttpContextIsNull()
    {
        // Arrange
        _httpContextAccessor.HttpContext.Returns((HttpContext?)null);
        var userContext = new UserContext(_httpContextAccessor);

        // Act & Assert
        userContext.UserId.Should().BeNull();
        userContext.IsAuthenticated.Should().BeFalse();
        userContext.SystemRole.Should().BeNull();
        userContext.IsSystemAdmin.Should().BeFalse();
    }

    [Fact]
    public void UserId_Should_ReturnNull_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        _httpContextAccessor.HttpContext.Returns(httpContext);
        var userContext = new UserContext(_httpContextAccessor);

        // Act & Assert
        userContext.UserId.Should().BeNull();
        userContext.IsAuthenticated.Should().BeFalse();
        userContext.SystemRole.Should().BeNull();
        userContext.IsSystemAdmin.Should().BeFalse();
    }

    [Fact]
    public void UserId_Should_ReturnGuid_WhenUserIsAuthenticatedWithNameIdentifierClaim()
    {
        // Arrange
        var expectedUserId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, expectedUserId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        _httpContextAccessor.HttpContext.Returns(httpContext);

        var userContext = new UserContext(_httpContextAccessor);

        // Act & Assert
        userContext.UserId.Should().Be(expectedUserId);
        userContext.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void SystemRole_Should_ReturnAdmin_And_IsSystemAdmin_True_WhenAdminClaimIsPresent()
    {
        // Arrange
        var expectedUserId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, expectedUserId.ToString()),
            new Claim("system_role", "Admin"),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        _httpContextAccessor.HttpContext.Returns(httpContext);

        var userContext = new UserContext(_httpContextAccessor);

        // Act & Assert
        userContext.SystemRole.Should().Be(SystemRole.Admin);
        userContext.IsSystemAdmin.Should().BeTrue();
    }

    [Fact]
    public void SystemRole_Should_ReturnUser_And_IsSystemAdmin_False_WhenUserClaimIsPresent()
    {
        // Arrange
        var expectedUserId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, expectedUserId.ToString()),
            new Claim("system_role", "User"),
            new Claim(ClaimTypes.Role, "User")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        _httpContextAccessor.HttpContext.Returns(httpContext);

        var userContext = new UserContext(_httpContextAccessor);

        // Act & Assert
        userContext.SystemRole.Should().Be(SystemRole.User);
        userContext.IsSystemAdmin.Should().BeFalse();
    }
}
