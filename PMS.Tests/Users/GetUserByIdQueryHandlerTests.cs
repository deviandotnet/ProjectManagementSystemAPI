using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Users.GetUserById;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.SharedKernel;
using Xunit;

namespace PMS.UnitTests.Users;

public class GetUserByIdQueryHandlerTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_Should_ReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(false);

        var handler = new GetUserByIdQueryHandler(context, userContext);
        var query = new GetUserByIdQuery(Guid.NewGuid());

        // Act
        Result<UserResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.Unauthorized);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);

        var nonExistentId = Guid.NewGuid();
        var handler = new GetUserByIdQueryHandler(context, userContext);
        var query = new GetUserByIdQuery(nonExistentId);

        // Act
        Result<UserResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.NotFoundById(nonExistentId));
    }

    [Fact]
    public async Task Handle_Should_ReturnDisplayOnlyUserDetails_WhenUserExists()
    {
        // Arrange
        await using var context = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            MiddleName = "Alexander",
            LastName = "Doe",
            Email = "john.doe@example.com",
            PasswordHash = "super_secret_hash",
            SystemRole = SystemRole.User,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);

        var handler = new GetUserByIdQueryHandler(context, userContext);
        var query = new GetUserByIdQuery(user.Id);

        // Act
        Result<UserResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(user.Id);
        result.Value.FirstName.Should().Be("John");
        result.Value.MiddleName.Should().Be("Alexander");
        result.Value.LastName.Should().Be("Doe");
        result.Value.Email.Should().Be("john.doe@example.com");
        result.Value.SystemRole.Should().Be(SystemRole.User);
        result.Value.IsActive.Should().BeTrue();
    }
}
