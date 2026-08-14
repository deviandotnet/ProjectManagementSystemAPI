using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Holidays.CreateHoliday;
using PMS.Domain.HolidayCalendars;
using PMS.Infrastructure.Database;
using PMS.SharedKernel;
using Xunit;

namespace PMS.UnitTests.Holidays;

public class CreateHolidayCommandHandlerTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_Should_ReturnForbidden_WhenNotSystemAdmin()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(Guid.NewGuid());
        userContext.IsSystemAdmin.Returns(false);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        var handler = new CreateHolidayCommandHandler(context, unitOfWork, userContext, dateTimeProvider);
        var command = new CreateHolidayCommand(new DateOnly(2026, 12, 24), "Company Christmas Eve", HolidayType.Company);

        // Act
        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(HolidayErrors.Forbidden);
    }

    [Fact]
    public async Task Handle_Should_CreateHoliday_WhenSystemAdmin()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(Guid.NewGuid());
        userContext.IsSystemAdmin.Returns(true);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        var handler = new CreateHolidayCommandHandler(context, unitOfWork, userContext, dateTimeProvider);
        var command = new CreateHolidayCommand(new DateOnly(2026, 12, 24), "Company Christmas Eve", HolidayType.Company, false, 2026);

        // Act
        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());

        HolidayCalendar? created = await context.HolidayCalendar.SingleOrDefaultAsync(h => h.Id == result.Value);
        created.Should().NotBeNull();
        created!.Name.Should().Be("Company Christmas Eve");
    }
}
