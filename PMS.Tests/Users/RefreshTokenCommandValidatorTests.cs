using FluentValidation.TestHelper;
using PMS.Application.Users.RefreshToken;

namespace PMS.UnitTests.Users;

public class RefreshTokenCommandValidatorTests
{
    private readonly RefreshTokenCommandValidator _validator = new();

    [Fact]
    public void Validate_Should_ReturnFailure_WhenTokenIsEmpty()
    {
        // Arrange
        var command = new RefreshTokenCommand(string.Empty);

        // Act
        TestValidationResult<RefreshTokenCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(candidate => candidate.Token);
    }

    [Fact]
    public void Validate_Should_ReturnFailure_WhenTokenExceedsMaximumLength()
    {
        // Arrange
        var command = new RefreshTokenCommand(new string('a', 201));

        // Act
        TestValidationResult<RefreshTokenCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(candidate => candidate.Token);
    }

    [Fact]
    public void Validate_Should_ReturnSuccess_WhenTokenIsValid()
    {
        // Arrange
        var command = new RefreshTokenCommand("valid_refresh_token");

        // Act
        TestValidationResult<RefreshTokenCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
