using FluentAssertions;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Features.UserFeatures;
using PMS.Application.Features.UserFeatures.CreateUser;
using PMS.Domain.Entities;
using PMS.Infrastructure.Data;
using PMS.UnitTests.Helpers;

namespace PMS.UnitTests.UserFeatures;

public class CreateUserHandlerTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IRepository<Users> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly CreateUserHandler _handler;

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
        var command = new CreateUserRequest(
            FirstName: "John",
            MiddleName: "Alexander",
            LastName: "Doe",
            Email: "john.doe@example.com",
            Password: "SecurePassword123!",
            CreatedByUserId: createdByUserId
        );

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().NotBeEmpty();
        result.Value.FirstName.Should().Be("John");
        result.Value.MiddleName.Should().Be("Alexander");
        result.Value.LastName.Should().Be("Doe");
        result.Value.Email.Should().Be("john.doe@example.com");
        result.Value.IsActive.Should().BeTrue();
        result.Value.CreatedByUserId.Should().Be(createdByUserId);

        _passwordHasher.Received(1).Hash("SecurePassword123!");
        await _repository.Received(1).AddAsync(
            Arg.Is<Users>(u =>
                u.FirstName == "John" &&
                u.MiddleName == "Alexander" &&
                u.LastName == "Doe" &&
                u.Email == "john.doe@example.com" &&
                u.PasswordHash == "hashed_SecurePassword123!" &&
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
        var command = new CreateUserRequest(
            FirstName: "Jane",
            MiddleName: null,
            LastName: "Smith",
            Email: "jane.smith@example.com",
            Password: "AnotherPassword123!"
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
        var existingEmail = "existing.user@example.com";
        _dbContext.Users.Add(new Users
        {
            Id = Guid.NewGuid(),
            FirstName = "Existing",
            LastName = "User",
            Email = existingEmail,
            PasswordHash = "hashed_pass",
            IsActive = true
        });
        await _dbContext.SaveChangesAsync();

        var command = new CreateUserRequest(
            FirstName: "New",
            MiddleName: null,
            LastName: "User",
            Email: existingEmail,
            Password: "Password123!"
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
        _dbContext.Users.Add(new Users
        {
            Id = Guid.NewGuid(),
            FirstName = "Existing",
            LastName = "User",
            Email = "user.test@domain.com",
            PasswordHash = "hashed_pass",
            IsActive = true
        });
        await _dbContext.SaveChangesAsync();

        var command = new CreateUserRequest(
            FirstName: "Duplicate",
            MiddleName: null,
            LastName: "Test",
            Email: "USER.TEST@DOMAIN.COM",
            Password: "Password123!"
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
