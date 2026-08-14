using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Calendar.CalculateWorkingDays;
using PMS.Domain.HolidayCalendars;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.SharedKernel;
using Xunit;

namespace PMS.UnitTests.Calendar;

public class CalculateWorkingDaysQueryHandlerTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_Should_ReturnWorkingDays_ExcludingWeekendsAndHolidays()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        context.Projects.Add(new Project
        {
            Id = projectId, Name = "P", Description = "D",
            StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31),
            CreatedByUserId = userId
        });
        context.ProjectMembers.Add(new ProjectMember
        {
            Id = Guid.NewGuid(), ProjectId = projectId, UserId = userId, Role = UserRole.Member
        });

        // Jan 1, 2026 is Thursday (New Year's Day)
        context.HolidayCalendar.Add(new HolidayCalendar
        {
            Id = Guid.NewGuid(),
            Name = "New Year's Day",
            HolidayDate = new DateOnly(2026, 1, 1),
            Type = HolidayType.National,
            IsRecurringAnnually = true
        });
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);
        userContext.IsSystemAdmin.Returns(false);

        var handler = new CalculateWorkingDaysQueryHandler(context, userContext);
        // Jan 1 to Jan 7 = 7 calendar days:
        // Jan 1: Thursday (Holiday)
        // Jan 2: Friday (Work)
        // Jan 3: Saturday (Weekend)
        // Jan 4: Sunday (Weekend)
        // Jan 5: Monday (Work)
        // Jan 6: Tuesday (Work)
        // Jan 7: Wednesday (Work)
        // Working days = 4 (Jan 2, 5, 6, 7)
        var query = new CalculateWorkingDaysQuery(projectId, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 7));

        // Act
        Result<WorkingDaysResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CalendarDays.Should().Be(7);
        result.Value.WorkingDays.Should().Be(4);
        result.Value.WeekendDays.Should().Be(2);
        result.Value.HolidayDays.Should().Be(1);
        result.Value.Holidays.Should().ContainSingle(h => h.Name == "New Year's Day");
    }

    [Fact]
    public async Task Handle_Should_ReturnInvalidDateRange_WhenEndDateBeforeStartDate()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        context.Projects.Add(new Project
        {
            Id = projectId, Name = "P", Description = "D",
            StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31),
            CreatedByUserId = userId
        });
        context.ProjectMembers.Add(new ProjectMember
        {
            Id = Guid.NewGuid(), ProjectId = projectId, UserId = userId, Role = UserRole.Member
        });
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);
        userContext.IsSystemAdmin.Returns(false);

        var handler = new CalculateWorkingDaysQueryHandler(context, userContext);
        var query = new CalculateWorkingDaysQuery(projectId, new DateOnly(2026, 1, 10), new DateOnly(2026, 1, 5));

        // Act
        Result<WorkingDaysResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(HolidayErrors.InvalidDateRange);
    }
}
