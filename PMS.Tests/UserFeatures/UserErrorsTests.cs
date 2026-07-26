using FluentAssertions;
using PMS.Application.Features.UserFeatures;
using PMS.Domain.Abstractions.Errors;

namespace PMS.UnitTests.UserFeatures;

public class UserErrorsTests
{
    [Fact]
    public void InvalidId_ShouldReturnValidationTypeError_WithExpectedCodeAndMessage()
    {
        // Arrange & Act
        var error = UserErrors.InvalidId;

        // Assert
        error.Should().NotBeNull();
        error.Code.Should().Be("User.InvalidId");
        error.Description.Should().Be("The provided User ID is not a valid GUID format.");
        error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void NotFound_ShouldReturnNotFoundErrorType_WithUserIdInMessage()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var error = UserErrors.NotFound(userId);

        // Assert
        error.Should().NotBeNull();
        error.Code.Should().Be("User.NotFound");
        error.Description.Should().Be($"User with ID '{userId}' was not found.");
        error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void EmailAlreadyExists_ShouldReturnConflictErrorType_WithEmailInMessage()
    {
        // Arrange
        var email = "test@example.com";

        // Act
        var error = UserErrors.EmailAlreadyExists(email);

        // Assert
        error.Should().NotBeNull();
        error.Code.Should().Be("User.EmailAlreadyExists");
        error.Description.Should().Be($"A user with the email '{email}' already exists.");
        error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public void NoUsersFound_ShouldReturnNotFoundErrorType_WithExpectedCodeAndMessage()
    {
        // Arrange & Act
        var error = UserErrors.NoUsersFound;

        // Assert
        error.Should().NotBeNull();
        error.Code.Should().Be("User.NoUsersFound");
        error.Description.Should().Be("No users were found.");
        error.Type.Should().Be(ErrorType.NotFound);
    }
}
