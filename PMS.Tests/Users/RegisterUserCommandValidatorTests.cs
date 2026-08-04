using FluentAssertions;
using PMS.Application.Users.RegisterUser;

namespace PMS.UnitTests.Users;

public class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _validator = new();

    [Theory]
    [InlineData("", "Doe", "valid@example.com", "Password123")]
    [InlineData("John", "", "valid@example.com", "Password123")]
    [InlineData("John", "Doe", "invalid-email", "Password123")]
    [InlineData("John", "Doe", "valid@example.com", "123")]
    public void Validate_Should_ReturnFailure_WhenFieldsAreInvalid(
        string firstName, string lastName, string email, string password)
    {
        // Arrange
        var command = new RegisterUserCommand(firstName, null, lastName, email, password);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Should_ReturnSuccess_WhenCommandIsValid()
    {
        // Arrange
        var command = new RegisterUserCommand("John", "A", "Doe", "john.doe@example.com", "Password123!");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
