using FluentValidation.TestHelper;
using PMS.Application.Holidays.UpdateHoliday;
using PMS.Domain.HolidayCalendars;
using Xunit;

namespace PMS.UnitTests.Holidays;

public class UpdateHolidayCommandValidatorTests
{
    private readonly UpdateHolidayCommandValidator _validator = new();

    [Fact]
    public void Should_HaveError_WhenIdIsEmpty()
    {
        var command = new UpdateHolidayCommand(Guid.Empty, new DateOnly(2026, 1, 1), "New Year", HolidayType.National);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Id);
    }

    [Fact]
    public void Should_NotHaveError_WhenValid()
    {
        var command = new UpdateHolidayCommand(Guid.NewGuid(), new DateOnly(2026, 1, 1), "New Year", HolidayType.National);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
