using FluentValidation.TestHelper;
using PMS.Application.Holidays.CreateHoliday;
using PMS.Domain.HolidayCalendars;
using Xunit;

namespace PMS.UnitTests.Holidays;

public class CreateHolidayCommandValidatorTests
{
    private readonly CreateHolidayCommandValidator _validator = new();

    [Fact]
    public void Should_HaveError_WhenNameIsEmpty()
    {
        var command = new CreateHolidayCommand(new DateOnly(2026, 1, 1), "", HolidayType.National);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(c => c.Name);
    }

    [Fact]
    public void Should_NotHaveError_WhenValid()
    {
        var command = new CreateHolidayCommand(new DateOnly(2026, 1, 1), "New Year", HolidayType.National);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
