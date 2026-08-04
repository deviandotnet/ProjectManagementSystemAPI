using FluentAssertions;
using PMS.Application.Users.LoginUser;
using Xunit;

namespace PMS.UnitTests.Users;

public class LoginUserCommandValidatorTests
{
    private readonly LoginUserCommandValidator _validator = new();

    [Theory]
    [InlineData("", "Password123")]
    [InlineData("invalid-email", "Password123")]
    [InlineData("valid@example.com", "")]
    [InlineData("valid@example.com", "123")]
    public void Validate_Should_ReturnFailure_WhenFieldsAreInvalid(string email, string password)
    {
        // Arrange
        var command = new LoginUserCommand(email, password);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Should_ReturnSuccess_WhenCommandIsValid()
    {
        // Arrange
        var command = new LoginUserCommand("john.doe@example.com", "Password123!");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
