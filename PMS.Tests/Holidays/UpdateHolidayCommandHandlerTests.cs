using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Holidays.UpdateHoliday;
using PMS.Domain.HolidayCalendars;
using PMS.Infrastructure.Database;
using PMS.SharedKernel;
using Xunit;

namespace PMS.UnitTests.Holidays;

public class UpdateHolidayCommandHandlerTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenHolidayDoesNotExist()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(Guid.NewGuid());
        userContext.IsSystemAdmin.Returns(true);
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var nonExistentId = Guid.NewGuid();
        var handler = new UpdateHolidayCommandHandler(context, unitOfWork, userContext);
        var command = new UpdateHolidayCommand(nonExistentId, new DateOnly(2026, 12, 24), "Updated Name", HolidayType.Company);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(HolidayErrors.NotFound(nonExistentId));
    }

    [Fact]
    public async Task Handle_Should_UpdateHoliday_WhenValid()
    {
        // Arrange
        await using var context = CreateDbContext();
        var holiday = new HolidayCalendar
        {
            Id = Guid.NewGuid(),
            Name = "Original Name",
            HolidayDate = new DateOnly(2026, 12, 24),
            Type = HolidayType.Company
        };
        context.HolidayCalendar.Add(holiday);
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(Guid.NewGuid());
        userContext.IsSystemAdmin.Returns(true);
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var handler = new UpdateHolidayCommandHandler(context, unitOfWork, userContext);
        var command = new UpdateHolidayCommand(holiday.Id, new DateOnly(2026, 12, 24), "Updated Company Holiday", HolidayType.Special);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());

        HolidayCalendar? updated = await context.HolidayCalendar.SingleOrDefaultAsync(h => h.Id == holiday.Id);
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated Company Holiday");
        updated.Type.Should().Be(HolidayType.Special);
    }
}
