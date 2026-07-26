using FluentAssertions;
using FluentValidation.TestHelper;
using PMS.Application.Features.ProjectFeatures.CreateProject;
using PMS.Domain.Enums;
using Xunit;

namespace PMS.UnitTests.ProjectFeatures;

public class CreateProjectValidatorTests
{
    private readonly CreateProjectValidator _validator = new();

    [Fact]
    public void Validate_WithValidRequest_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var request = new CreateProjectRequest(
            Name: "Website Redesign",
            Description: "Redesign corporate website",
            StartDate: new DateOnly(2025, 1, 1),
            EndDate: new DateOnly(2025, 6, 30),
            WeekStartDay: 1,
            DefaultTimelineScale: TimelineScale.Daily,
            CreatedByUserId: Guid.NewGuid()
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithNullDescription_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var request = new CreateProjectRequest(
            Name: "Simple Project",
            Description: null,
            StartDate: new DateOnly(2025, 1, 1),
            EndDate: new DateOnly(2025, 3, 31),
            WeekStartDay: 1,
            DefaultTimelineScale: TimelineScale.Weekly,
            CreatedByUserId: Guid.NewGuid()
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithSameStartAndEndDate_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var sameDate = new DateOnly(2025, 5, 15);
        var request = new CreateProjectRequest(
            Name: "One Day Event",
            Description: "Single day project",
            StartDate: sameDate,
            EndDate: sameDate,
            WeekStartDay: 1,
            DefaultTimelineScale: TimelineScale.Daily,
            CreatedByUserId: Guid.NewGuid()
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithWeekStartDaySunday_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var request = new CreateProjectRequest(
            Name: "Sunday Start Project",
            Description: "Starts on Sunday",
            StartDate: new DateOnly(2025, 1, 1),
            EndDate: new DateOnly(2025, 2, 1),
            WeekStartDay: 0,
            DefaultTimelineScale: TimelineScale.Daily,
            CreatedByUserId: Guid.NewGuid()
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithWeekStartDaySaturday_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var request = new CreateProjectRequest(
            Name: "Saturday Start Project",
            Description: "Starts on Saturday",
            StartDate: new DateOnly(2025, 1, 1),
            EndDate: new DateOnly(2025, 2, 1),
            WeekStartDay: 6,
            DefaultTimelineScale: TimelineScale.Daily,
            CreatedByUserId: Guid.NewGuid()
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_With200CharacterName_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var maxLengthName = new string('A', 200);
        var request = new CreateProjectRequest(
            Name: maxLengthName,
            Description: "Max length name project",
            StartDate: new DateOnly(2025, 1, 1),
            EndDate: new DateOnly(2025, 2, 1),
            WeekStartDay: 1,
            DefaultTimelineScale: TimelineScale.Daily,
            CreatedByUserId: Guid.NewGuid()
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithNullName_ShouldHaveValidationError()
    {
        // Arrange
        var request = new CreateProjectRequest(
            Name: null!,
            Description: "Project with null name",
            StartDate: new DateOnly(2025, 1, 1),
            EndDate: new DateOnly(2025, 2, 1),
            WeekStartDay: 1,
            DefaultTimelineScale: TimelineScale.Daily,
            CreatedByUserId: Guid.NewGuid()
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Project name is required.");
    }

    [Fact]
    public void Validate_WithEmptyName_ShouldHaveValidationError()
    {
        // Arrange
        var request = new CreateProjectRequest(
            Name: "",
            Description: "Project with empty name",
            StartDate: new DateOnly(2025, 1, 1),
            EndDate: new DateOnly(2025, 2, 1),
            WeekStartDay: 1,
            DefaultTimelineScale: TimelineScale.Daily,
            CreatedByUserId: Guid.NewGuid()
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Project name is required.");
    }

    [Fact]
    public void Validate_WithWhitespaceName_ShouldHaveValidationError()
    {
        // Arrange
        var request = new CreateProjectRequest(
            Name: "   ",
            Description: "Project with whitespace name",
            StartDate: new DateOnly(2025, 1, 1),
            EndDate: new DateOnly(2025, 2, 1),
            WeekStartDay: 1,
            DefaultTimelineScale: TimelineScale.Daily,
            CreatedByUserId: Guid.NewGuid()
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Project name is required.");
    }

    [Fact]
    public void Validate_WithNameExceeding200Characters_ShouldHaveValidationError()
    {
        // Arrange
        var overLengthName = new string('A', 201);
        var request = new CreateProjectRequest(
            Name: overLengthName,
            Description: "Overlength name project",
            StartDate: new DateOnly(2025, 1, 1),
            EndDate: new DateOnly(2025, 2, 1),
            WeekStartDay: 1,
            DefaultTimelineScale: TimelineScale.Daily,
            CreatedByUserId: Guid.NewGuid()
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Project name must not exceed 200 characters.");
    }

    [Fact]
    public void Validate_WithEndDateBeforeStartDate_ShouldHaveValidationError()
    {
        // Arrange
        var request = new CreateProjectRequest(
            Name: "Invalid Date Project",
            Description: "End date before start date",
            StartDate: new DateOnly(2025, 6, 30),
            EndDate: new DateOnly(2025, 1, 1),
            WeekStartDay: 1,
            DefaultTimelineScale: TimelineScale.Daily,
            CreatedByUserId: Guid.NewGuid()
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EndDate)
              .WithErrorMessage("End date must be on or after the start date.");
    }

    [Fact]
    public void Validate_WithNegativeWeekStartDay_ShouldHaveValidationError()
    {
        // Arrange
        var request = new CreateProjectRequest(
            Name: "Invalid Week Start Project",
            Description: "Negative week start day",
            StartDate: new DateOnly(2025, 1, 1),
            EndDate: new DateOnly(2025, 2, 1),
            WeekStartDay: -1,
            DefaultTimelineScale: TimelineScale.Daily,
            CreatedByUserId: Guid.NewGuid()
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WeekStartDay)
              .WithErrorMessage("Week start day must be between 0 (Sunday) and 6 (Saturday).");
    }

    [Fact]
    public void Validate_WithWeekStartDayGreaterThanSix_ShouldHaveValidationError()
    {
        // Arrange
        var request = new CreateProjectRequest(
            Name: "Invalid Week Start Project",
            Description: "Week start day is 7",
            StartDate: new DateOnly(2025, 1, 1),
            EndDate: new DateOnly(2025, 2, 1),
            WeekStartDay: 7,
            DefaultTimelineScale: TimelineScale.Daily,
            CreatedByUserId: Guid.NewGuid()
        );

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WeekStartDay)
              .WithErrorMessage("Week start day must be between 0 (Sunday) and 6 (Saturday).");
    }
}
