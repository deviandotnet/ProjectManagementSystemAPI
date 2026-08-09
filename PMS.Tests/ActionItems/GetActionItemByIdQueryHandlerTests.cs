using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.ActionItems.GetActionItemById;
using PMS.Domain.ActionItems;
using PMS.Domain.ActualExecutions;
using PMS.Domain.Categories;
using PMS.Domain.PlannedSchedules;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.SharedKernel;
using Xunit;

namespace PMS.UnitTests.ActionItems;

public class GetActionItemByIdQueryHandlerTests
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

        var handler = new GetActionItemByIdQueryHandler(context, userContext, dateTimeProvider);
        var query = new GetActionItemByIdQuery(Guid.NewGuid(), Guid.NewGuid());

        // Act
        Result<ActionItemResponse> result = await handler.Handle(query, CancellationToken.None);

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

        var nonExistentProjectId = Guid.NewGuid();
        var handler = new GetActionItemByIdQueryHandler(context, userContext, dateTimeProvider);
        var query = new GetActionItemByIdQuery(nonExistentProjectId, Guid.NewGuid());

        // Act
        Result<ActionItemResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProjectErrors.NotFound(nonExistentProjectId));
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenActionItemDoesNotExist()
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
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();

        var nonExistentActionItemId = Guid.NewGuid();
        var handler = new GetActionItemByIdQueryHandler(context, userContext, dateTimeProvider);
        var query = new GetActionItemByIdQuery(projectId, nonExistentActionItemId);

        // Act
        Result<ActionItemResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ActionItemErrors.NotFound(nonExistentActionItemId));
    }

    [Fact]
    public async Task Handle_Should_ReturnActionItemDetails_WhenValid()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var actionItemId = Guid.NewGuid();

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
        context.Categories.Add(new Category { Id = categoryId, ProjectId = projectId, Name = "Category 1" });
        context.ActionItems.Add(new ActionItem
        {
            Id = actionItemId, ProjectId = projectId, CategoryId = categoryId,
            ActionItemName = "Specific Task", Priority = Priority.High, Sequence = 1
        });
        context.PlannedSchedules.Add(new PlannedSchedule
        {
            Id = Guid.NewGuid(), ActionItemId = actionItemId,
            PlannedStartDate = new DateOnly(2026, 1, 1), PlannedEndDate = new DateOnly(2026, 1, 31),
            PlannedStartWeek = "WW01", PlannedEndWeek = "WW05",
            DurationCalendarDays = 30, DurationWorkingDays = 22
        });
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);
        userContext.IsSystemAdmin.Returns(false);
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(new DateTime(2026, 1, 15));

        var handler = new GetActionItemByIdQueryHandler(context, userContext, dateTimeProvider);
        var query = new GetActionItemByIdQuery(projectId, actionItemId);

        // Act
        Result<ActionItemResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(actionItemId);
        result.Value.ActionItemName.Should().Be("Specific Task");
        result.Value.CategoryName.Should().Be("Category 1");
        result.Value.PlannedSchedule.Should().NotBeNull();
        result.Value.PlannedSchedule!.PlannedStartWeek.Should().Be("WW01");
        result.Value.ComputedStatus.Should().Be((int)ActionItemStatus.Plan);
    }
}
