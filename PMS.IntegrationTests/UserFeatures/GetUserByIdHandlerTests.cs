using Bogus;
using FluentAssertions;
using PMS.Application.Features.UserFeatures;
using PMS.Application.Features.UserFeatures.GetUserById;
using PMS.Domain.Entities;
using PMS.Infrastructure.Data;
using PMS.IntegrationTests.Helpers;

namespace PMS.IntegrationTests.UserFeatures;

public class GetUserByIdHandlerTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly GetUserByIdHandler _handler;
    private readonly Faker _faker = new();

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
        var firstName = _faker.Name.FirstName();
        var middleName = _faker.Name.FirstName();
        var lastName = _faker.Name.LastName();
        var email = _faker.Internet.Email();

        var userEntity = new Users
        {
            Id = userId,
            FirstName = firstName,
            MiddleName = middleName,
            LastName = lastName,
            Email = email,
            PasswordHash = _faker.Random.Hash(),
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
        result.Value.FirstName.Should().Be(firstName);
        result.Value.MiddleName.Should().Be(middleName);
        result.Value.LastName.Should().Be(lastName);
        result.Value.Email.Should().Be(email);
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
