using Bogus;
using FluentAssertions;
using PMS.Application.Features.UserFeatures;
using PMS.Application.Features.UserFeatures.GetAllUsers;
using PMS.Domain.Entities;
using PMS.Infrastructure.Data;
using PMS.IntegrationTests.Helpers;

namespace PMS.IntegrationTests.UserFeatures;

public class GetAllUsersHandlerTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly GetAllUsersHandler _handler;
    private readonly Faker _faker = new();

    public GetAllUsersHandlerTests()
    {
        _dbContext = TestDbContextFactory.Create();
        _handler = new GetAllUsersHandler(_dbContext);
    }

    [Fact]
    public async Task HandleAsync_WhenUsersExist_ShouldReturnSuccessResultWithListOfUsers()
    {
        // Arrange
        var email1 = _faker.Internet.Email();
        var email2 = _faker.Internet.Email();

        _dbContext.Users.AddRange(
            new Users
            {
                Id = Guid.NewGuid(),
                FirstName = _faker.Name.FirstName(),
                LastName = _faker.Name.LastName(),
                Email = email1,
                PasswordHash = _faker.Random.Hash(),
                IsActive = true
            },
            new Users
            {
                Id = Guid.NewGuid(),
                FirstName = _faker.Name.FirstName(),
                LastName = _faker.Name.LastName(),
                Email = email2,
                PasswordHash = _faker.Random.Hash(),
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
        result.Value.Select(u => u.Email).Should().Contain(new[] { email1, email2 });
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
