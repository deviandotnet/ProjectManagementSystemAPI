using FluentAssertions;
using FluentValidation.TestHelper;
using PMS.Application.Features.ProjectFeatures.GetProjectById;
using Xunit;

namespace PMS.UnitTests.ProjectFeatures;

public class GetProjectByIdValidatorTests
{
    private readonly GetProjectByIdValidator _validator = new();

    [Fact]
    public void Validate_WithValidProjectId_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var request = new GetProjectByIdRequest(Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyGuidProjectId_ShouldHaveValidationError()
    {
        // Arrange
        var request = new GetProjectByIdRequest(Guid.Empty);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProjectId)
              .WithErrorMessage("Project ID must not be empty.");
    }
}
