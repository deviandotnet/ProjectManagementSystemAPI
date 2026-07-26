using FluentAssertions;
using PMS.Application.Features.UserFeatures;
using PMS.Application.Features.UserFeatures.GetAllUsers;
using PMS.Domain.Entities;
using PMS.Infrastructure.Data;
using PMS.UnitTests.Helpers;

namespace PMS.UnitTests.UserFeatures;

public class GetAllUsersHandlerTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly GetAllUsersHandler _handler;

    public GetAllUsersHandlerTests()
    {
        _dbContext = TestDbContextFactory.Create();
        _handler = new GetAllUsersHandler(_dbContext);
    }

    [Fact]
    public async Task HandleAsync_WhenUsersExist_ShouldReturnSuccessResultWithListOfUsers()
    {
        // Arrange
        _dbContext.Users.AddRange(
            new Users
            {
                Id = Guid.NewGuid(),
                FirstName = "UserOne",
                LastName = "Test",
                Email = "user1@example.com",
                PasswordHash = "hash1",
                IsActive = true
            },
            new Users
            {
                Id = Guid.NewGuid(),
                FirstName = "UserTwo",
                LastName = "Test",
                Email = "user2@example.com",
                PasswordHash = "hash2",
                IsActive = true
            }
        );
        await _dbContext.SaveChangesAsync();

        var request = new GetAllUsersRequest();

        // Act
        var result = await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCount(2);
        result.Value.Select(u => u.Email).Should().Contain(new[] { "user1@example.com", "user2@example.com" });
    }

    [Fact]
    public async Task HandleAsync_WhenNoUsersExist_ShouldReturnNoUsersFoundError()
    {
        // Arrange
        var request = new GetAllUsersRequest();

        // Act
        var result = await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(UserErrors.NoUsersFound);
        result.Error.Code.Should().Be("User.NoUsersFound");
        result.Error.Type.Should().Be(Domain.Abstractions.Errors.ErrorType.NotFound);
    }
}
