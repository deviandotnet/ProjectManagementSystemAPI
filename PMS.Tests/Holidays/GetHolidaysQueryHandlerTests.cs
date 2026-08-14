using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Holidays.GetHolidays;
using PMS.Domain.HolidayCalendars;
using PMS.Infrastructure.Database;
using PMS.SharedKernel;
using Xunit;

namespace PMS.UnitTests.Holidays;

public class GetHolidaysQueryHandlerTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_Should_ReturnHolidays_WhenAuthenticated()
    {
        // Arrange
        await using var context = CreateDbContext();
        context.HolidayCalendar.AddRange(
            new HolidayCalendar { Id = Guid.NewGuid(), Name = "Holiday 1", HolidayDate = new DateOnly(2026, 1, 1), Type = HolidayType.National },
            new HolidayCalendar { Id = Guid.NewGuid(), Name = "Holiday 2", HolidayDate = new DateOnly(2026, 5, 1), Type = HolidayType.Company }
        );
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(Guid.NewGuid());

        var handler = new GetHolidaysQueryHandler(context, userContext);
        var query = new GetHolidaysQuery();

        // Act
        Result<IReadOnlyCollection<HolidayResponse>> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }
}
