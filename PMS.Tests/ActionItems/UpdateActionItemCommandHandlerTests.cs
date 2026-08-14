using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.ActionItems.UpdateActionItem;
using PMS.Domain.ActionItems;
using PMS.Domain.Categories;
using PMS.Domain.PlannedSchedules;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.SharedKernel;
using Xunit;

namespace PMS.UnitTests.ActionItems;

public class UpdateActionItemCommandHandlerTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
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
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var nonExistentActionItemId = Guid.NewGuid();
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        var handler = new UpdateActionItemCommandHandler(context, unitOfWork, userContext, dateTimeProvider);
        var command = new UpdateActionItemCommand(
            projectId, nonExistentActionItemId, Guid.NewGuid(), null, "Task", null,
            Priority.Medium, null, null, null, 1, null,
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 10));

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ActionItemErrors.NotFound(nonExistentActionItemId));
    }

    [Fact]
    public async Task Handle_Should_UpdateActionItemAndSchedule_WhenValid()
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
        context.Categories.Add(new Category { Id = categoryId, ProjectId = projectId, Name = "Cat 1" });
        context.ActionItems.Add(new ActionItem
        {
            Id = actionItemId, ProjectId = projectId, CategoryId = categoryId,
            ActionItemName = "Original Name", Priority = Priority.Low, Sequence = 1
        });
        context.PlannedSchedules.Add(new PlannedSchedule
        {
            Id = Guid.NewGuid(), ActionItemId = actionItemId,
            PlannedStartDate = new DateOnly(2026, 1, 1), PlannedEndDate = new DateOnly(2026, 1, 10),
            PlannedStartWeek = "WW01", PlannedEndWeek = "WW02",
            DurationCalendarDays = 10, DurationWorkingDays = 7
        });
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);
        userContext.IsSystemAdmin.Returns(false);
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        var handler = new UpdateActionItemCommandHandler(context, unitOfWork, userContext, dateTimeProvider);
        var command = new UpdateActionItemCommand(
            projectId, actionItemId, categoryId, null, "Updated Name", "Updated Desc",
            Priority.Critical, "Jane Dev", userId, 20m, 2, "Updated Remarks",
            new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 20));

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());

        ActionItem? updatedItem = await context.ActionItems.SingleOrDefaultAsync(a => a.Id == actionItemId);
        updatedItem.Should().NotBeNull();
        updatedItem!.ActionItemName.Should().Be("Updated Name");
        updatedItem.Priority.Should().Be(Priority.Critical);
        updatedItem.DomainEvents.Should().ContainSingle(e => e is ActionItemUpdatedDomainEvent);

        PlannedSchedule? updatedSchedule = await context.PlannedSchedules.SingleOrDefaultAsync(s => s.ActionItemId == actionItemId);
        updatedSchedule.Should().NotBeNull();
        updatedSchedule!.PlannedStartWeek.Should().Be("WW02");
        updatedSchedule.PlannedEndWeek.Should().Be("WW04");
    }
}
