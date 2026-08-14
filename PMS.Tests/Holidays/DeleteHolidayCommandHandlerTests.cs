using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Holidays.DeleteHoliday;
using PMS.Domain.HolidayCalendars;
using PMS.Infrastructure.Database;
using PMS.SharedKernel;
using Xunit;

namespace PMS.UnitTests.Holidays;

public class DeleteHolidayCommandHandlerTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_Should_DeleteHoliday_WhenSystemAdmin()
    {
        // Arrange
        await using var context = CreateDbContext();
        var holiday = new HolidayCalendar
        {
            Id = Guid.NewGuid(),
            Name = "Company Day",
            HolidayDate = new DateOnly(2026, 7, 1),
            Type = HolidayType.Company
        };
        context.HolidayCalendar.Add(holiday);
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(Guid.NewGuid());
        userContext.IsSystemAdmin.Returns(true);
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var handler = new DeleteHolidayCommandHandler(context, unitOfWork, userContext);
        var command = new DeleteHolidayCommand(holiday.Id);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());

        HolidayCalendar? deleted = await context.HolidayCalendar.SingleOrDefaultAsync(h => h.Id == holiday.Id);
        deleted.Should().BeNull();
    }
}
