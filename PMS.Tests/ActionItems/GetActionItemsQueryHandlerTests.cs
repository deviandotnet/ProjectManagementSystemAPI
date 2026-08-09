using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.ActionItems.GetActionItems;
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

public class GetActionItemsQueryHandlerTests
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

        var handler = new GetActionItemsQueryHandler(context, userContext, dateTimeProvider);
        var query = new GetActionItemsQuery(Guid.NewGuid());

        // Act
        Result<IReadOnlyCollection<ActionItemResponse>> result =
            await handler.Handle(query, CancellationToken.None);

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
        var handler = new GetActionItemsQueryHandler(context, userContext, dateTimeProvider);
        var query = new GetActionItemsQuery(nonExistentProjectId);

        // Act
        Result<IReadOnlyCollection<ActionItemResponse>> result =
            await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProjectErrors.NotFound(nonExistentProjectId));
    }

    [Fact]
    public async Task Handle_Should_ReturnNotProjectMember_WhenUserIsNotAMember()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userId = Guid.NewGuid();
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Project",
            Description = "Desc",
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            CreatedByUserId = Guid.NewGuid()
        };
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);
        userContext.IsSystemAdmin.Returns(false);
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();

        var handler = new GetActionItemsQueryHandler(context, userContext, dateTimeProvider);
        var query = new GetActionItemsQuery(project.Id);

        // Act
        Result<IReadOnlyCollection<ActionItemResponse>> result =
            await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ActionItemErrors.NotProjectMember);
    }

    [Fact]
    public async Task Handle_Should_ReturnEmptyList_WhenNoActionItemsExist()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userId = Guid.NewGuid();
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Project",
            Description = "Desc",
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            CreatedByUserId = userId
        };
        var member = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = userId,
            Role = UserRole.Member
        };
        context.Projects.Add(project);
        context.ProjectMembers.Add(member);
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);
        userContext.IsSystemAdmin.Returns(false);
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        var handler = new GetActionItemsQueryHandler(context, userContext, dateTimeProvider);
        var query = new GetActionItemsQuery(project.Id);

        // Act
        Result<IReadOnlyCollection<ActionItemResponse>> result =
            await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_ReturnActionItems_WithComputedStatusPlan_WhenNoScheduleExists()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var project = new Project
        {
            Id = projectId,
            Name = "Project",
            Description = "Desc",
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            CreatedByUserId = userId
        };
        var member = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            Role = UserRole.Member
        };
        var category = new Category { Id = categoryId, ProjectId = projectId, Name = "Planning" };
        var actionItem = new ActionItem
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            CategoryId = categoryId,
            ActionItemName = "Task 1",
            Priority = Priority.Medium,
            Sequence = 1
        };

        context.Projects.Add(project);
        context.ProjectMembers.Add(member);
        context.Categories.Add(category);
        context.ActionItems.Add(actionItem);
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);
        userContext.IsSystemAdmin.Returns(false);
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        var handler = new GetActionItemsQueryHandler(context, userContext, dateTimeProvider);
        var query = new GetActionItemsQuery(projectId);

        // Act
        Result<IReadOnlyCollection<ActionItemResponse>> result =
            await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);

        ActionItemResponse item = result.Value.First();
        item.ActionItemName.Should().Be("Task 1");
        item.CategoryName.Should().Be("Planning");
        item.ComputedStatus.Should().Be((int)ActionItemStatus.Plan);
        item.ComputedStatusLabel.Should().Be("Plan");
        item.PlannedSchedule.Should().BeNull();
        item.ActualExecution.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_ComputeOngoing_WhenActualStartExistsButNoActualEnd()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var actionItemId = Guid.NewGuid();

        var project = new Project
        {
            Id = projectId, Name = "P", Description = "D",
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            CreatedByUserId = userId
        };
        context.Projects.Add(project);
        context.ProjectMembers.Add(new ProjectMember
        {
            Id = Guid.NewGuid(), ProjectId = projectId, UserId = userId, Role = UserRole.Member
        });
        context.Categories.Add(new Category { Id = categoryId, ProjectId = projectId, Name = "Cat" });
        context.ActionItems.Add(new ActionItem
        {
            Id = actionItemId, ProjectId = projectId, CategoryId = categoryId,
            ActionItemName = "Ongoing Task", Priority = Priority.High, Sequence = 1
        });
        context.PlannedSchedules.Add(new PlannedSchedule
        {
            Id = Guid.NewGuid(), ActionItemId = actionItemId,
            PlannedStartDate = new DateOnly(2026, 1, 1),
            PlannedEndDate = new DateOnly(2026, 6, 30),
            PlannedStartWeek = "WW01", PlannedEndWeek = "WW26",
            DurationCalendarDays = 180, DurationWorkingDays = 130
        });
        context.ActualExecutions.Add(new ActualExecution
        {
            Id = Guid.NewGuid(), ActionItemId = actionItemId,
            ActualStartDate = new DateOnly(2026, 1, 5),
            ActualEndDate = null
        });
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);
        userContext.IsSystemAdmin.Returns(false);
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(new DateTime(2026, 3, 15));

        var handler = new GetActionItemsQueryHandler(context, userContext, dateTimeProvider);
        var query = new GetActionItemsQuery(projectId);

        // Act
        Result<IReadOnlyCollection<ActionItemResponse>> result =
            await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ActionItemResponse item = result.Value.First();
        item.ComputedStatus.Should().Be((int)ActionItemStatus.Ongoing);
        item.ComputedStatusLabel.Should().Be("Ongoing");
        item.PlannedSchedule.Should().NotBeNull();
        item.ActualExecution.Should().NotBeNull();
        item.ActualExecution!.ActualStartDate.Should().Be(new DateOnly(2026, 1, 5));
        item.ActualExecution.ActualEndDate.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Should_ComputeDelayed_WhenTodayPastPlannedEndAndNotStarted()
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
        context.Categories.Add(new Category { Id = categoryId, ProjectId = projectId, Name = "Cat" });
        context.ActionItems.Add(new ActionItem
        {
            Id = actionItemId, ProjectId = projectId, CategoryId = categoryId,
            ActionItemName = "Delayed Task", Priority = Priority.Critical, Sequence = 1
        });
        context.PlannedSchedules.Add(new PlannedSchedule
        {
            Id = Guid.NewGuid(), ActionItemId = actionItemId,
            PlannedStartDate = new DateOnly(2026, 1, 1),
            PlannedEndDate = new DateOnly(2026, 1, 31),
            PlannedStartWeek = "WW01", PlannedEndWeek = "WW05",
            DurationCalendarDays = 30, DurationWorkingDays = 22
        });
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);
        userContext.IsSystemAdmin.Returns(false);
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(new DateTime(2026, 3, 1)); // Past PlannedEndDate

        var handler = new GetActionItemsQueryHandler(context, userContext, dateTimeProvider);
        var query = new GetActionItemsQuery(projectId);

        // Act
        Result<IReadOnlyCollection<ActionItemResponse>> result =
            await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ActionItemResponse item = result.Value.First();
        item.ComputedStatus.Should().Be((int)ActionItemStatus.Delayed);
        item.ComputedStatusLabel.Should().Be("Delayed");
    }

    [Fact]
    public async Task Handle_Should_ComputeCompletedEarly_WhenActualEndBeforePlannedEnd()
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
        context.Categories.Add(new Category { Id = categoryId, ProjectId = projectId, Name = "Cat" });
        context.ActionItems.Add(new ActionItem
        {
            Id = actionItemId, ProjectId = projectId, CategoryId = categoryId,
            ActionItemName = "Early Task", Priority = Priority.Low, Sequence = 1
        });
        context.PlannedSchedules.Add(new PlannedSchedule
        {
            Id = Guid.NewGuid(), ActionItemId = actionItemId,
            PlannedStartDate = new DateOnly(2026, 1, 1),
            PlannedEndDate = new DateOnly(2026, 1, 31),
            PlannedStartWeek = "WW01", PlannedEndWeek = "WW05",
            DurationCalendarDays = 30, DurationWorkingDays = 22
        });
        context.ActualExecutions.Add(new ActualExecution
        {
            Id = Guid.NewGuid(), ActionItemId = actionItemId,
            ActualStartDate = new DateOnly(2026, 1, 1),
            ActualEndDate = new DateOnly(2026, 1, 20) // Before PlannedEndDate (Jan 31)
        });
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);
        userContext.IsSystemAdmin.Returns(false);
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(new DateTime(2026, 2, 1));

        var handler = new GetActionItemsQueryHandler(context, userContext, dateTimeProvider);

        // Act
        Result<IReadOnlyCollection<ActionItemResponse>> result =
            await handler.Handle(new GetActionItemsQuery(projectId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.First().ComputedStatus.Should().Be((int)ActionItemStatus.CompletedEarly);
    }

    [Fact]
    public async Task Handle_Should_ComputeCompletedOnTime_WhenActualEndEqualsPlannedEnd()
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
        context.Categories.Add(new Category { Id = categoryId, ProjectId = projectId, Name = "Cat" });
        context.ActionItems.Add(new ActionItem
        {
            Id = actionItemId, ProjectId = projectId, CategoryId = categoryId,
            ActionItemName = "On Time Task", Priority = Priority.Medium, Sequence = 1
        });
        context.PlannedSchedules.Add(new PlannedSchedule
        {
            Id = Guid.NewGuid(), ActionItemId = actionItemId,
            PlannedStartDate = new DateOnly(2026, 1, 1),
            PlannedEndDate = new DateOnly(2026, 1, 31),
            PlannedStartWeek = "WW01", PlannedEndWeek = "WW05",
            DurationCalendarDays = 30, DurationWorkingDays = 22
        });
        context.ActualExecutions.Add(new ActualExecution
        {
            Id = Guid.NewGuid(), ActionItemId = actionItemId,
            ActualStartDate = new DateOnly(2026, 1, 1),
            ActualEndDate = new DateOnly(2026, 1, 31) // Equals PlannedEndDate
        });
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);
        userContext.IsSystemAdmin.Returns(false);
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(new DateTime(2026, 2, 1));

        var handler = new GetActionItemsQueryHandler(context, userContext, dateTimeProvider);

        // Act
        Result<IReadOnlyCollection<ActionItemResponse>> result =
            await handler.Handle(new GetActionItemsQuery(projectId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.First().ComputedStatus.Should().Be((int)ActionItemStatus.CompletedOntime);
    }

    [Fact]
    public async Task Handle_Should_ComputeCompletedLate_WhenActualEndAfterPlannedEnd()
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
        context.Categories.Add(new Category { Id = categoryId, ProjectId = projectId, Name = "Cat" });
        context.ActionItems.Add(new ActionItem
        {
            Id = actionItemId, ProjectId = projectId, CategoryId = categoryId,
            ActionItemName = "Late Task", Priority = Priority.High, Sequence = 1
        });
        context.PlannedSchedules.Add(new PlannedSchedule
        {
            Id = Guid.NewGuid(), ActionItemId = actionItemId,
            PlannedStartDate = new DateOnly(2026, 1, 1),
            PlannedEndDate = new DateOnly(2026, 1, 31),
            PlannedStartWeek = "WW01", PlannedEndWeek = "WW05",
            DurationCalendarDays = 30, DurationWorkingDays = 22
        });
        context.ActualExecutions.Add(new ActualExecution
        {
            Id = Guid.NewGuid(), ActionItemId = actionItemId,
            ActualStartDate = new DateOnly(2026, 1, 1),
            ActualEndDate = new DateOnly(2026, 2, 15) // After PlannedEndDate (Jan 31)
        });
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);
        userContext.IsSystemAdmin.Returns(false);
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(new DateTime(2026, 3, 1));

        var handler = new GetActionItemsQueryHandler(context, userContext, dateTimeProvider);

        // Act
        Result<IReadOnlyCollection<ActionItemResponse>> result =
            await handler.Handle(new GetActionItemsQuery(projectId), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.First().ComputedStatus.Should().Be((int)ActionItemStatus.CompletedLate);
    }

    [Fact]
    public async Task Handle_Should_FilterByStatusParameter_WhenProvided()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

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
        context.Categories.Add(new Category { Id = categoryId, ProjectId = projectId, Name = "Cat" });

        // Action item 1: Plan (no schedule)
        context.ActionItems.Add(new ActionItem
        {
            Id = Guid.NewGuid(), ProjectId = projectId, CategoryId = categoryId,
            ActionItemName = "Plan Item", Priority = Priority.Low, Sequence = 1
        });

        // Action item 2: Ongoing
        var ongoingId = Guid.NewGuid();
        context.ActionItems.Add(new ActionItem
        {
            Id = ongoingId, ProjectId = projectId, CategoryId = categoryId,
            ActionItemName = "Ongoing Item", Priority = Priority.Medium, Sequence = 2
        });
        context.PlannedSchedules.Add(new PlannedSchedule
        {
            Id = Guid.NewGuid(), ActionItemId = ongoingId,
            PlannedStartDate = new DateOnly(2026, 1, 1), PlannedEndDate = new DateOnly(2026, 6, 30),
            PlannedStartWeek = "WW01", PlannedEndWeek = "WW26",
            DurationCalendarDays = 180, DurationWorkingDays = 130
        });
        context.ActualExecutions.Add(new ActualExecution
        {
            Id = Guid.NewGuid(), ActionItemId = ongoingId,
            ActualStartDate = new DateOnly(2026, 1, 5), ActualEndDate = null
        });
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);
        userContext.IsSystemAdmin.Returns(false);
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(new DateTime(2026, 3, 1));

        var handler = new GetActionItemsQueryHandler(context, userContext, dateTimeProvider);

        // Filter only Ongoing (status = 1)
        var query = new GetActionItemsQuery(projectId, Statuses: [1]);

        // Act
        Result<IReadOnlyCollection<ActionItemResponse>> result =
            await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value.First().ActionItemName.Should().Be("Ongoing Item");
        result.Value.First().ComputedStatus.Should().Be((int)ActionItemStatus.Ongoing);
    }

    [Fact]
    public async Task Handle_Should_AllowSystemAdmin_WithoutProjectMembership()
    {
        // Arrange
        await using var context = CreateDbContext();
        var adminId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        context.Projects.Add(new Project
        {
            Id = projectId, Name = "P", Description = "D",
            StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31),
            CreatedByUserId = Guid.NewGuid()
        });
        context.Categories.Add(new Category { Id = categoryId, ProjectId = projectId, Name = "Cat" });
        context.ActionItems.Add(new ActionItem
        {
            Id = Guid.NewGuid(), ProjectId = projectId, CategoryId = categoryId,
            ActionItemName = "Admin View", Priority = Priority.Medium, Sequence = 1
        });
        await context.SaveChangesAsync();

        // System admin - NOT a project member
        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(adminId);
        userContext.IsSystemAdmin.Returns(true);
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        var handler = new GetActionItemsQueryHandler(context, userContext, dateTimeProvider);
        var query = new GetActionItemsQuery(projectId);

        // Act
        Result<IReadOnlyCollection<ActionItemResponse>> result =
            await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }
}
