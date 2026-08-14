using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Projects.GetTimeline;
using PMS.Domain.ActionItems;
using PMS.Domain.ActualExecutions;
using PMS.Domain.Categories;
using PMS.Domain.PlannedSchedules;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.SubCategories;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.SharedKernel;
using Xunit;

namespace PMS.UnitTests.Projects;

public class GetTimelineQueryHandlerTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_Should_ReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(false);
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();

        var handler = new GetTimelineQueryHandler(context, userContext, dateTimeProvider);
        var query = new GetTimelineQuery(Guid.NewGuid());

        // Act
        Result<TimelineResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.Unauthorized);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenProjectDoesNotExist()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(Guid.NewGuid());
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();

        var handler = new GetTimelineQueryHandler(context, userContext, dateTimeProvider);
        var query = new GetTimelineQuery(Guid.NewGuid());

        // Act
        Result<TimelineResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Should_ReturnTimelineData_WhenUserIsMember()
    {
        // Arrange
        await using var context = CreateDbContext();
        Guid userId = Guid.NewGuid();
        DateOnly startDate = new(2026, 1, 5);
        DateOnly endDate = new(2026, 1, 25);

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Timeline Project",
            Description = "Desc",
            StartDate = startDate,
            EndDate = endDate,
            WeekStartDay = 1,
            DefaultTimelineScale = TimelineScale.Weekly,
            CreatedByUserId = userId
        };

        var member = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = userId,
            Role = UserRole.Member
        };

        var category = new Category
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Name = "Architecture",
            DisplayOrder = 1,
            Color = "#3A86FF"
        };

        var subCategory = new SubCategory
        {
            Id = Guid.NewGuid(),
            CategoryId = category.Id,
            Name = "Backend",
            DisplayOrder = 1
        };

        var actionItem = new ActionItem
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            CategoryId = category.Id,
            SubCategoryId = subCategory.Id,
            ActionItemName = "Setup Timeline API",
            Sequence = 1
        };

        var plannedSchedule = new PlannedSchedule
        {
            Id = Guid.NewGuid(),
            ActionItemId = actionItem.Id,
            PlannedStartDate = new DateOnly(2026, 1, 5),
            PlannedEndDate = new DateOnly(2026, 1, 15)
        };

        context.Projects.Add(project);
        context.ProjectMembers.Add(member);
        context.Categories.Add(category);
        context.SubCategories.Add(subCategory);
        context.ActionItems.Add(actionItem);
        context.PlannedSchedules.Add(plannedSchedule);
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);

        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(new DateTime(2026, 1, 10));

        var handler = new GetTimelineQueryHandler(context, userContext, dateTimeProvider);
        var query = new GetTimelineQuery(project.Id);

        // Act
        Result<TimelineResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        TimelineResponse response = result.Value;

        response.ProjectId.Should().Be(project.Id);
        response.Scale.Should().Be("Weekly");
        response.Columns.Should().NotBeEmpty();

        response.Rows.Should().HaveCount(3); // Category, SubCategory, ActionItem
        response.Rows[0].RowType.Should().Be("Category");
        response.Rows[0].Label.Should().Be("Architecture");

        response.Rows[1].RowType.Should().Be("SubCategory");
        response.Rows[1].Label.Should().Be("Backend");

        response.Rows[2].RowType.Should().Be("ActionItem");
        response.Rows[2].Label.Should().Be("Setup Timeline API");
        response.Rows[2].PlannedStartWeekIndex.Should().Be(0);
        response.Rows[2].PlannedEndWeekIndex.Should().Be(1);
    }
}
