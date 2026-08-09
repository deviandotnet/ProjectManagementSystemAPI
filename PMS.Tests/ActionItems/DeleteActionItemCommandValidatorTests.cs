using FluentValidation.TestHelper;
using PMS.Application.ActionItems.DeleteActionItem;
using Xunit;

namespace PMS.UnitTests.ActionItems;

public class DeleteActionItemCommandValidatorTests
{
    private readonly DeleteActionItemCommandValidator _validator = new();

    [Fact]
    public void Should_HaveError_WhenActionItemIdIsEmpty()
    {
        // Arrange
        var command = new DeleteActionItemCommand(Guid.NewGuid(), Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.ActionItemId);
    }

    [Fact]
    public void Should_NotHaveError_WhenCommandIsValid()
    {
        // Arrange
        var command = new DeleteActionItemCommand(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
