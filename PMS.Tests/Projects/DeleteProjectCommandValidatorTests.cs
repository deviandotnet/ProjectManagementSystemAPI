using FluentAssertions;
using PMS.Application.Projects.DeleteProject;
using Xunit;

namespace PMS.UnitTests.Projects;

public class DeleteProjectCommandValidatorTests
{
    private readonly DeleteProjectCommandValidator _validator = new();

    [Fact]
    public void Validate_Should_Fail_WhenIdIsEmpty()
    {
        // Arrange
        var command = new DeleteProjectCommand(Guid.Empty);

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(DeleteProjectCommand.Id));
    }

    [Fact]
    public void Validate_Should_Pass_WhenIdIsValid()
    {
        // Arrange
        var command = new DeleteProjectCommand(Guid.NewGuid());

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
