using FluentValidation.TestHelper;
using PMS.Application.Holidays.DeleteHoliday;
using Xunit;

namespace PMS.UnitTests.Holidays;

public class DeleteHolidayCommandValidatorTests
{
    private readonly DeleteHolidayCommandValidator _validator = new();

    [Fact]
    public void Should_HaveError_WhenIdIsEmpty()
    {
        var command = new DeleteHolidayCommand(Guid.Empty);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Id);
    }

    [Fact]
    public void Should_NotHaveError_WhenValid()
    {
        var command = new DeleteHolidayCommand(Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
