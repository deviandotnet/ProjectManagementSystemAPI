using FluentValidation.TestHelper;
using PMS.Application.ActionItems.CreateActionItem;
using PMS.Domain.ActionItems;
using Xunit;

namespace PMS.UnitTests.ActionItems;

public class CreateActionItemCommandValidatorTests
{
    private readonly CreateActionItemCommandValidator _validator = new();

    [Fact]
    public void Should_HaveError_WhenActionItemNameIsEmpty()
    {
        // Arrange
        var command = new CreateActionItemCommand(
            Guid.NewGuid(), Guid.NewGuid(), null, "", null,
            Priority.Medium, null, null, null, 1, null,
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 10));

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.ActionItemName);
    }

    [Fact]
    public void Should_HaveError_WhenPlannedEndDateBeforePlannedStartDate()
    {
        // Arrange
        var command = new CreateActionItemCommand(
            Guid.NewGuid(), Guid.NewGuid(), null, "Valid Name", null,
            Priority.Medium, null, null, null, 1, null,
            new DateOnly(2026, 1, 15), new DateOnly(2026, 1, 5));

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.PlannedEndDate);
    }

    [Fact]
    public void Should_NotHaveError_WhenCommandIsValid()
    {
        // Arrange
        var command = new CreateActionItemCommand(
            Guid.NewGuid(), Guid.NewGuid(), null, "Valid Name", "Description",
            Priority.High, "Owner", Guid.NewGuid(), 25m, 1, "Remarks",
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 15));

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
