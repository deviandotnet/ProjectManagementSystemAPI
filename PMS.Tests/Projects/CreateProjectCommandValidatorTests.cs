using FluentAssertions;
using PMS.Application.Projects.CreateProject;
using PMS.SharedKernel;

namespace PMS.UnitTests.Projects;

public class CreateProjectCommandValidatorTests
{
    private readonly CreateProjectCommandValidator _validator = new();

    [Fact]
    public void Validate_Should_Fail_WhenNameIsEmpty()
    {
        // Arrange
        var command = new CreateProjectCommand(
            Name: "",
            Description: "Valid description",
            StartDate: DateOnly.FromDateTime(DateTime.Today),
            EndDate: DateOnly.FromDateTime(DateTime.Today.AddDays(10))
        );

        // Act
        var validationResult = _validator.Validate(command);

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProjectCommand.Name));
    }

    [Fact]
    public void Validate_Should_Fail_WhenEndDateIsBeforeStartDate()
    {
        // Arrange
        var command = new CreateProjectCommand(
            Name: "Valid Name",
            Description: "Valid description",
            StartDate: DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            EndDate: DateOnly.FromDateTime(DateTime.Today)
        );

        // Act
        var validationResult = _validator.Validate(command);

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProjectCommand.EndDate));
    }

    [Fact]
    public void Validate_Should_Fail_WhenWeekStartDayOutOfRange()
    {
        // Arrange
        var command = new CreateProjectCommand(
            Name: "Valid Name",
            Description: "Valid description",
            StartDate: DateOnly.FromDateTime(DateTime.Today),
            EndDate: DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            WeekStartDay: 7 // Out of range 0..6
        );

        // Act
        var validationResult = _validator.Validate(command);

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProjectCommand.WeekStartDay));
    }

    [Fact]
    public void Validate_Should_Pass_WhenCommandIsValid()
    {
        // Arrange
        var command = new CreateProjectCommand(
            Name: "Valid Project",
            Description: "Valid description",
            StartDate: DateOnly.FromDateTime(DateTime.Today),
            EndDate: DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            WeekStartDay: 1,
            DefaultTimelineScale: TimelineScale.Weekly,
            ProgressMode: ProgressMode.CountBased
        );

        // Act
        var validationResult = _validator.Validate(command);

        // Assert
        validationResult.IsValid.Should().BeTrue();
    }
}
