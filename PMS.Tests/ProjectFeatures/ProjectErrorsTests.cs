using FluentAssertions;
using PMS.Application.Features.ProjectFeatures;
using PMS.Domain.Abstractions.Errors;
using Xunit;

namespace PMS.UnitTests.ProjectFeatures;

public class ProjectErrorsTests
{
    [Fact]
    public void InvalidId_ShouldReturnValidationTypeError_WithExpectedCodeAndMessage()
    {
        // Arrange & Act
        var error = ProjectErrors.InvalidId;

        // Assert
        error.Should().NotBeNull();
        error.Code.Should().Be("Project.InvalidId");
        error.Description.Should().Be("The provided Project ID is not a valid GUID format.");
        error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void NotFound_ShouldReturnNotFoundErrorType_WithProjectIdInMessage()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        // Act
        var error = ProjectErrors.NotFound(projectId);

        // Assert
        error.Should().NotBeNull();
        error.Code.Should().Be("Project.NotFound");
        error.Description.Should().Be($"Project with ID '{projectId}' was not found.");
        error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void NoProjectsFound_ShouldReturnNotFoundErrorType_WithExpectedCodeAndMessage()
    {
        // Arrange & Act
        var error = ProjectErrors.NoProjectsFound;

        // Assert
        error.Should().NotBeNull();
        error.Code.Should().Be("Project.NoProjectsFound");
        error.Description.Should().Be("No projects were found.");
        error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void NameAlreadyExists_ShouldReturnConflictErrorType_WithProjectNameInMessage()
    {
        // Arrange
        var projectName = "Alpha Redesign";

        // Act
        var error = ProjectErrors.NameAlreadyExists(projectName);

        // Assert
        error.Should().NotBeNull();
        error.Code.Should().Be("Project.NameAlreadyExists");
        error.Description.Should().Be($"A project with the name '{projectName}' already exists.");
        error.Type.Should().Be(ErrorType.Conflict);
    }
}
