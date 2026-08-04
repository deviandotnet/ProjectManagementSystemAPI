using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Users;
using PMS.Application.Users.LoginUser;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.SharedKernel;
using Xunit;

namespace PMS.UnitTests.Users;

public class LoginUserCommandHandlerTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        await using var context = CreateDbContext();
        var hasher = Substitute.For<IPasswordHasher>();
        var tokenProvider = Substitute.For<ITokenProvider>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();

        var handler = new LoginUserCommandHandler(context, hasher, tokenProvider, dateTimeProvider);
        var command = new LoginUserCommand("nonexistent@example.com", "Password123!");

        // Act
        Result<AccessTokenResponse> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.NotFoundByEmail);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenPasswordIsInvalid()
    {
        // Arrange
        await using var context = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane.doe@example.com",
            PasswordHash = "hashed_correct_password"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var hasher = Substitute.For<IPasswordHasher>();
        hasher.Verify("WrongPassword!", "hashed_correct_password").Returns(false);

        var tokenProvider = Substitute.For<ITokenProvider>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();

        var handler = new LoginUserCommandHandler(context, hasher, tokenProvider, dateTimeProvider);
        var command = new LoginUserCommand("jane.doe@example.com", "WrongPassword!");

        // Act
        Result<AccessTokenResponse> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.NotFoundByEmail);
    }

    [Fact]
    public async Task Handle_Should_ReturnTokens_WhenCredentialsAreValid()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            PasswordHash = "hashed_valid_password"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var hasher = Substitute.For<IPasswordHasher>();
        hasher.Verify("ValidPassword123!", "hashed_valid_password").Returns(true);

        var tokenProvider = Substitute.For<ITokenProvider>();
        tokenProvider.CreateAccessToken(Arg.Any<User>()).Returns("access_token_123");
        tokenProvider.CreateRefreshToken().Returns("refresh_token_456");

        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        var handler = new LoginUserCommandHandler(context, hasher, tokenProvider, dateTimeProvider);
        var command = new LoginUserCommand("john.doe@example.com", "ValidPassword123!");

        // Act
        Result<AccessTokenResponse> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.AccessToken.Should().Be("access_token_123");
        result.Value.RefreshToken.Should().Be("refresh_token_456");

        var refreshTokenInDb = await context.RefreshTokens.FirstOrDefaultAsync(r => r.UserId == userId);
        refreshTokenInDb.Should().NotBeNull();
        refreshTokenInDb!.Token.Should().Be("refresh_token_456");
    }
}
