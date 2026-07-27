using Bogus;
using FluentAssertions;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Features.UserFeatures;
using PMS.Application.Features.UserFeatures.CreateUser;
using PMS.Domain.Entities;
using PMS.Infrastructure.Data;
using PMS.IntegrationTests.Helpers;

namespace PMS.IntegrationTests.UserFeatures;

public class CreateUserHandlerTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IRepository<Users> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly CreateUserHandler _handler;
    private readonly Faker _faker = new();

    public CreateUserHandlerTests()
    {
        _dbContext = TestDbContextFactory.Create();
        _repository = Substitute.For<IRepository<Users>>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _passwordHasher = Substitute.For<IPasswordHasher>();

        _passwordHasher.Hash(Arg.Any<string>()).Returns(callInfo => $"hashed_{callInfo.Arg<string>()}");

        _handler = new CreateUserHandler(
            _dbContext,
            _repository,
            _unitOfWork,
            _passwordHasher);
    }

    [Fact]
    public async Task HandleAsync_WithValidRequest_ShouldCreateUserAndReturnSuccess()
    {
        // Arrange
        var createdByUserId = Guid.NewGuid();
        var firstName = _faker.Name.FirstName();
        var middleName = _faker.Name.FirstName();
        var lastName = _faker.Name.LastName();
        var email = _faker.Internet.Email();
        var password = _faker.Internet.Password(length: 12);

        var command = new CreateUserRequest(
            FirstName: firstName,
            MiddleName: middleName,
            LastName: lastName,
            Email: email,
            Password: password,
            CreatedByUserId: createdByUserId
        );

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().NotBeEmpty();
        result.Value.FirstName.Should().Be(firstName);
        result.Value.MiddleName.Should().Be(middleName);
        result.Value.LastName.Should().Be(lastName);
        result.Value.Email.Should().Be(email.ToLowerInvariant());
        result.Value.IsActive.Should().BeTrue();
        result.Value.CreatedByUserId.Should().Be(createdByUserId);

        _passwordHasher.Received(1).Hash(password);
        await _repository.Received(1).AddAsync(
            Arg.Is<Users>(u =>
                u.FirstName == firstName &&
                u.MiddleName == middleName &&
                u.LastName == lastName &&
                u.Email == email.ToLowerInvariant() &&
                u.PasswordHash == $"hashed_{password}" &&
                u.IsActive &&
                u.CreatedByUserId == createdByUserId
            ),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithNullMiddleNameAndNullCreatedByUserId_ShouldCreateUserWithDefaults()
    {
        // Arrange
        var firstName = _faker.Name.FirstName();
        var lastName = _faker.Name.LastName();
        var email = _faker.Internet.Email();
        var password = _faker.Internet.Password(length: 12);

        var command = new CreateUserRequest(
            FirstName: firstName,
            MiddleName: null,
            LastName: lastName,
            Email: email,
            Password: password
        );

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MiddleName.Should().BeNull();
        result.Value.CreatedByUserId.Should().BeNull();

        await _repository.Received(1).AddAsync(
            Arg.Is<Users>(u => u.MiddleName == null && u.CreatedByUserId == null),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenEmailAlreadyExists_ShouldReturnConflictError()
    {
        // Arrange
        var existingEmail = _faker.Internet.Email();
        _dbContext.Users.Add(new Users
        {
            Id = Guid.NewGuid(),
            FirstName = _faker.Name.FirstName(),
            LastName = _faker.Name.LastName(),
            Email = existingEmail,
            PasswordHash = _faker.Random.Hash(),
            IsActive = true
        });
        await _dbContext.SaveChangesAsync();

        var command = new CreateUserRequest(
            FirstName: _faker.Name.FirstName(),
            MiddleName: null,
            LastName: _faker.Name.LastName(),
            Email: existingEmail,
            Password: _faker.Internet.Password(length: 12)
        );

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(UserErrors.EmailAlreadyExists(existingEmail));
        result.Error.Code.Should().Be("User.EmailAlreadyExists");
        result.Error.Type.Should().Be(Domain.Abstractions.Errors.ErrorType.Conflict);

        await _repository.DidNotReceive().AddAsync(Arg.Any<Users>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenEmailMatchesCaseInsensitively_ShouldReturnConflictError()
    {
        // Arrange
        var existingEmail = _faker.Internet.Email().ToLowerInvariant();
        _dbContext.Users.Add(new Users
        {
            Id = Guid.NewGuid(),
            FirstName = _faker.Name.FirstName(),
            LastName = _faker.Name.LastName(),
            Email = existingEmail,
            PasswordHash = _faker.Random.Hash(),
            IsActive = true
        });
        await _dbContext.SaveChangesAsync();

        var command = new CreateUserRequest(
            FirstName: _faker.Name.FirstName(),
            MiddleName: null,
            LastName: _faker.Name.LastName(),
            Email: existingEmail.ToUpperInvariant(),
            Password: _faker.Internet.Password(length: 12)
        );

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("User.EmailAlreadyExists");

        await _repository.DidNotReceive().AddAsync(Arg.Any<Users>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}
