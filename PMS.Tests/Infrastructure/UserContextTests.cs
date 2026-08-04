using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
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
    public void UserId_Should_ReturnGuid_WhenUserIsAuthenticatedWithSubClaim()
    {
        // Arrange
        var expectedUserId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim("sub", expectedUserId.ToString())
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
}
