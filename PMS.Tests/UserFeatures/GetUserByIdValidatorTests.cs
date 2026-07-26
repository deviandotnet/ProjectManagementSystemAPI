using FluentAssertions;
using FluentValidation.TestHelper;
using PMS.Application.Features.UserFeatures.GetUserById;

namespace PMS.UnitTests.UserFeatures;

public class GetUserByIdValidatorTests
{
    private readonly GetUserByIdValidator _validator = new();

    [Fact]
    public void Validate_WithValidUserId_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var request = new GetUserByIdRequest(Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyGuidUserId_ShouldHaveValidationError()
    {
        // Arrange
        var request = new GetUserByIdRequest(Guid.Empty);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
              .WithErrorMessage("User ID must not be empty.");
    }
}
