using FluentAssertions;
using PMS.Application.Projects.UpdateProject;
using PMS.Domain.Projects;
using PMS.SharedKernel;
using Xunit;

namespace PMS.UnitTests.Projects;

public class UpdateProjectCommandValidatorTests
{
    private readonly UpdateProjectCommandValidator _validator = new();

    [Fact]
    public void Validate_Should_Fail_WhenNameIsEmpty()
    {
        // Arrange
        var command = new UpdateProjectCommand(
            Id: Guid.NewGuid(),
            Name: "",
            Description: "Desc",
            StartDate: DateOnly.FromDateTime(DateTime.Today),
            EndDate: DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            WeekStartDay: 1,
            DefaultTimelineScale: TimelineScale.Weekly,
            ProgressMode: ProgressMode.CountBased,
            Status: ProjectStatus.Active
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProjectCommand.Name));
    }

    [Fact]
    public void Validate_Should_Fail_WhenEndDateIsBeforeStartDate()
    {
        // Arrange
        var command = new UpdateProjectCommand(
            Id: Guid.NewGuid(),
            Name: "Valid Name",
            Description: "Desc",
            StartDate: DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            EndDate: DateOnly.FromDateTime(DateTime.Today),
            WeekStartDay: 1,
            DefaultTimelineScale: TimelineScale.Weekly,
            ProgressMode: ProgressMode.CountBased,
            Status: ProjectStatus.Active
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_Should_Fail_WhenWeekStartDayIsInvalid()
    {
        // Arrange
        var command = new UpdateProjectCommand(
            Id: Guid.NewGuid(),
            Name: "Valid Name",
            Description: "Desc",
            StartDate: DateOnly.FromDateTime(DateTime.Today),
            EndDate: DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            WeekStartDay: 7, // Invalid, must be 0..6
            DefaultTimelineScale: TimelineScale.Weekly,
            ProgressMode: ProgressMode.CountBased,
            Status: ProjectStatus.Active
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProjectCommand.WeekStartDay));
    }

    [Fact]
    public void Validate_Should_Pass_WhenCommandIsValid()
    {
        // Arrange
        var command = new UpdateProjectCommand(
            Id: Guid.NewGuid(),
            Name: "Valid Name",
            Description: "Valid Description",
            StartDate: DateOnly.FromDateTime(DateTime.Today),
            EndDate: DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            WeekStartDay: 1,
            DefaultTimelineScale: TimelineScale.Weekly,
            ProgressMode: ProgressMode.CountBased,
            Status: ProjectStatus.Active
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
