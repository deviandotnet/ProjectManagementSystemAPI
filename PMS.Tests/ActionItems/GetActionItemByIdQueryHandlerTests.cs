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
using PMS.Domain.SubCategories;
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
        var subCategoryId = Guid.NewGuid();
        var actionItemId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var executionId = Guid.NewGuid();

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
        context.SubCategories.Add(new SubCategory
        {
            Id = subCategoryId,
            CategoryId = categoryId,
            Name = "Subcategory 1"
        });
        context.ActionItems.Add(new ActionItem
        {
            Id = actionItemId, ProjectId = projectId, CategoryId = categoryId,
            SubCategoryId = subCategoryId, ActionItemName = "Specific Task",
            Priority = Priority.High, OwnerName = "Jane Owner", Sequence = 7,
            Weight = 25.5m, Remarks = "Projected remarks"
        });
        context.PlannedSchedules.Add(new PlannedSchedule
        {
            Id = scheduleId, ActionItemId = actionItemId,
            PlannedStartDate = new DateOnly(2026, 1, 1), PlannedEndDate = new DateOnly(2026, 1, 31),
            PlannedStartWeek = "WW01", PlannedEndWeek = "WW05",
            DurationCalendarDays = 30, DurationWorkingDays = 22
        });
        context.ActualExecutions.Add(new ActualExecution
        {
            Id = executionId,
            ActionItemId = actionItemId,
            ActualHours = 12.5m,
            DelayReason = "Waiting for approval"
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
        result.Value.CategoryId.Should().Be(categoryId);
        result.Value.CategoryName.Should().Be("Category 1");
        result.Value.SubCategoryId.Should().Be(subCategoryId);
        result.Value.SubCategoryName.Should().Be("Subcategory 1");
        result.Value.Priority.Should().Be((int)Priority.High);
        result.Value.OwnerName.Should().Be("Jane Owner");
        result.Value.Sequence.Should().Be(7);
        result.Value.PlannedSchedule.Should().NotBeNull();
        result.Value.PlannedSchedule!.Id.Should().Be(scheduleId);
        result.Value.PlannedSchedule!.PlannedStartWeek.Should().Be("WW01");
        result.Value.PlannedSchedule.PlannedEndWeek.Should().Be("WW05");
        result.Value.PlannedSchedule.DurationCalendarDays.Should().Be(30);
        result.Value.PlannedSchedule.DurationWorkingDays.Should().Be(22);
        result.Value.ActualExecution.Should().NotBeNull();
        result.Value.ActualExecution!.Id.Should().Be(executionId);
        result.Value.ActualExecution.ActualHours.Should().Be(12.5m);
        result.Value.ActualExecution.DelayReason.Should().Be("Waiting for approval");
        result.Value.ComputedStatus.Should().Be((int)ActionItemStatus.Plan);
        result.Value.ComputedStatusLabel.Should().Be(nameof(ActionItemStatus.Plan));
        result.Value.Weight.Should().Be(25.5m);
        result.Value.Remarks.Should().Be("Projected remarks");
    }

    [Fact]
    public async Task Handle_Should_ReturnNullOptionalDetailsAndPlanStatus_WhenScheduleAndExecutionDoNotExist()
    {
        // Arrange
        await using var context = CreateDbContext();
        (Guid userId, Guid projectId, Guid actionItemId) = await SeedActionItemAsync(context);
        IUserContext userContext = CreateUserContext(userId);
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(new DateTime(2026, 2, 1));
        var handler = new GetActionItemByIdQueryHandler(context, userContext, dateTimeProvider);

        // Act
        Result<ActionItemResponse> result = await handler.Handle(
            new GetActionItemByIdQuery(projectId, actionItemId),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PlannedSchedule.Should().BeNull();
        result.Value.ActualExecution.Should().BeNull();
        result.Value.SubCategoryId.Should().BeNull();
        result.Value.SubCategoryName.Should().BeNull();
        result.Value.ComputedStatus.Should().Be((int)ActionItemStatus.Plan);
    }

    [Fact]
    public async Task Handle_Should_ReturnOngoingStatus_WhenActualStartExistsWithoutActualEnd()
    {
        // Arrange
        await using var context = CreateDbContext();
        (Guid userId, Guid projectId, Guid actionItemId) = await SeedActionItemAsync(
            context,
            includeSchedule: true,
            actualStartDate: new DateOnly(2026, 1, 5));
        IUserContext userContext = CreateUserContext(userId);
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(new DateTime(2026, 1, 15));
        var handler = new GetActionItemByIdQueryHandler(context, userContext, dateTimeProvider);

        // Act
        Result<ActionItemResponse> result = await handler.Handle(
            new GetActionItemByIdQuery(projectId, actionItemId),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ComputedStatus.Should().Be((int)ActionItemStatus.Ongoing);
        result.Value.ComputedStatusLabel.Should().Be(nameof(ActionItemStatus.Ongoing));
    }

    [Theory]
    [InlineData(9, ActionItemStatus.CompletedEarly)]
    [InlineData(10, ActionItemStatus.CompletedOntime)]
    [InlineData(11, ActionItemStatus.CompletedLate)]
    public async Task Handle_Should_ReturnCompletedStatus_WhenActualEndExists(
        int actualEndDay,
        ActionItemStatus expectedStatus)
    {
        // Arrange
        await using var context = CreateDbContext();
        (Guid userId, Guid projectId, Guid actionItemId) = await SeedActionItemAsync(
            context,
            includeSchedule: true,
            actualStartDate: new DateOnly(2026, 1, 2),
            actualEndDate: new DateOnly(2026, 1, actualEndDay));
        IUserContext userContext = CreateUserContext(userId);
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(new DateTime(2026, 1, 15));
        var handler = new GetActionItemByIdQueryHandler(context, userContext, dateTimeProvider);

        // Act
        Result<ActionItemResponse> result = await handler.Handle(
            new GetActionItemByIdQuery(projectId, actionItemId),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ComputedStatus.Should().Be((int)expectedStatus);
        result.Value.ComputedStatusLabel.Should().Be(expectedStatus.ToString());
    }

    [Fact]
    public async Task Handle_Should_ReturnNotProjectMember_WhenUserIsNotAMember()
    {
        // Arrange
        await using var context = CreateDbContext();
        (_, Guid projectId, Guid actionItemId) = await SeedActionItemAsync(
            context,
            includeMember: false);
        IUserContext userContext = CreateUserContext(Guid.NewGuid());
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var handler = new GetActionItemByIdQueryHandler(context, userContext, dateTimeProvider);

        // Act
        Result<ActionItemResponse> result = await handler.Handle(
            new GetActionItemByIdQuery(projectId, actionItemId),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ActionItemErrors.NotProjectMember);
    }

    [Fact]
    public async Task Handle_Should_ReturnActionItem_WhenSystemAdminIsNotAProjectMember()
    {
        // Arrange
        await using var context = CreateDbContext();
        (_, Guid projectId, Guid actionItemId) = await SeedActionItemAsync(
            context,
            includeMember: false);
        IUserContext userContext = CreateUserContext(Guid.NewGuid(), isSystemAdmin: true);
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(new DateTime(2026, 1, 1));
        var handler = new GetActionItemByIdQueryHandler(context, userContext, dateTimeProvider);

        // Act
        Result<ActionItemResponse> result = await handler.Handle(
            new GetActionItemByIdQuery(projectId, actionItemId),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(actionItemId);
    }

    private static IUserContext CreateUserContext(Guid userId, bool isSystemAdmin = false)
    {
        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);
        userContext.IsSystemAdmin.Returns(isSystemAdmin);
        return userContext;
    }

    private static async Task<(Guid UserId, Guid ProjectId, Guid ActionItemId)> SeedActionItemAsync(
        ApplicationDbContext context,
        bool includeMember = true,
        bool includeSchedule = false,
        DateOnly? actualStartDate = null,
        DateOnly? actualEndDate = null)
    {
        Guid userId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();
        Guid actionItemId = Guid.NewGuid();

        context.Projects.Add(new Project
        {
            Id = projectId,
            Name = "Projection Project",
            Description = "Description",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31),
            CreatedByUserId = userId
        });
        context.Categories.Add(new Category
        {
            Id = categoryId,
            ProjectId = projectId,
            Name = "Projection Category"
        });
        context.ActionItems.Add(new ActionItem
        {
            Id = actionItemId,
            ProjectId = projectId,
            CategoryId = categoryId,
            ActionItemName = "Projected Item",
            Priority = Priority.Medium,
            Sequence = 1
        });

        if (includeMember)
        {
            context.ProjectMembers.Add(new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                UserId = userId,
                Role = UserRole.Member
            });
        }

        if (includeSchedule)
        {
            context.PlannedSchedules.Add(new PlannedSchedule
            {
                Id = Guid.NewGuid(),
                ActionItemId = actionItemId,
                PlannedStartDate = new DateOnly(2026, 1, 1),
                PlannedEndDate = new DateOnly(2026, 1, 10),
                PlannedStartWeek = "WW01",
                PlannedEndWeek = "WW02",
                DurationCalendarDays = 10,
                DurationWorkingDays = 8
            });
        }

        if (actualStartDate.HasValue || actualEndDate.HasValue)
        {
            context.ActualExecutions.Add(new ActualExecution
            {
                Id = Guid.NewGuid(),
                ActionItemId = actionItemId,
                ActualStartDate = actualStartDate,
                ActualEndDate = actualEndDate
            });
        }

        await context.SaveChangesAsync();
        return (userId, projectId, actionItemId);
    }
}
