using FluentAssertions;
using PMS.Application.Features.UserFeatures;
using PMS.Application.Features.UserFeatures.GetUserById;
using PMS.Domain.Entities;
using PMS.Infrastructure.Data;
using PMS.UnitTests.Helpers;

namespace PMS.UnitTests.UserFeatures;

public class GetUserByIdHandlerTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly GetUserByIdHandler _handler;

    public GetUserByIdHandlerTests()
    {
        _dbContext = TestDbContextFactory.Create();
        _handler = new GetUserByIdHandler(_dbContext);
    }

    [Fact]
    public async Task HandleAsync_WhenUserExists_ShouldReturnSuccessResultWithUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var createdByUserId = Guid.NewGuid();
        var userEntity = new Users
        {
            Id = userId,
            FirstName = "Alice",
            MiddleName = "Marie",
            LastName = "Wonderland",
            Email = "alice@example.com",
            PasswordHash = "secret_hash",
            IsActive = true,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _dbContext.Users.Add(userEntity);
        await _dbContext.SaveChangesAsync();

        var request = new GetUserByIdRequest(userId);

        // Act
        var result = await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(userId);
        result.Value.FirstName.Should().Be("Alice");
        result.Value.MiddleName.Should().Be("Marie");
        result.Value.LastName.Should().Be("Wonderland");
        result.Value.Email.Should().Be("alice@example.com");
        result.Value.IsActive.Should().BeTrue();
        result.Value.CreatedByUserId.Should().Be(createdByUserId);
    }

    [Fact]
    public async Task HandleAsync_WhenUserDoesNotExist_ShouldReturnNotFoundError()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();
        var request = new GetUserByIdRequest(nonExistentUserId);

        // Act
        var result = await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(UserErrors.NotFound(nonExistentUserId));
        result.Error.Code.Should().Be("User.NotFound");
        result.Error.Type.Should().Be(Domain.Abstractions.Errors.ErrorType.NotFound);
    }
}
