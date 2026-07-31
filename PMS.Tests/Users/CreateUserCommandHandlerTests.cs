using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Users.CreateUser;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.SharedKernel;

namespace PMS.UnitTests.Users;

public class CreateUserCommandHandlerTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_Should_ReturnConflict_WhenEmailAlreadyExists()
    {
        // Arrange
        await using var context = CreateDbContext();
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            PasswordHash = "hashed_pass"
        };
        context.Users.Add(existingUser);
        await context.SaveChangesAsync();

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var passwordHasher = Substitute.For<IPasswordHasher>();
        var handler = new CreateUserCommandHandler(context, unitOfWork, passwordHasher);

        var command = new CreateUserCommand(
            FirstName: "John",
            MiddleName: null,
            LastName: "Doe",
            Email: "JOHN.DOE@EXAMPLE.COM",
            Password: "Password123!"
        );

        // Act
        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.EmailAlreadyExists("JOHN.DOE@EXAMPLE.COM"));
    }

    [Fact]
    public async Task Handle_Should_CreateUserAndRaiseDomainEvent_WhenValid()
    {
        // Arrange
        await using var context = CreateDbContext();
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var passwordHasher = Substitute.For<IPasswordHasher>();
        passwordHasher.Hash("SecurePass123!").Returns("hashed_secure_pass");

        var handler = new CreateUserCommandHandler(context, unitOfWork, passwordHasher);

        var command = new CreateUserCommand(
            FirstName: "Alice",
            MiddleName: "M",
            LastName: "Smith",
            Email: "alice.smith@example.com",
            Password: "SecurePass123!"
        );

        // Act
        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());

        User? createdUser = await context.Users.SingleOrDefaultAsync(u => u.Id == result.Value);
        createdUser.Should().NotBeNull();
        createdUser!.FirstName.Should().Be("Alice");
        createdUser.MiddleName.Should().Be("M");
        createdUser.LastName.Should().Be("Smith");
        createdUser.Email.Should().Be("alice.smith@example.com");
        createdUser.PasswordHash.Should().Be("hashed_secure_pass");
        createdUser.IsActive.Should().BeTrue();
        createdUser.DomainEvents.Should().ContainSingle(e => e is UserCreatedDomainEvent);
    }
}
