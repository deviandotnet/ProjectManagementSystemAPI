using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.ActionItems.DeleteActionItem;
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

public class DeleteActionItemCommandHandlerTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_Should_ReturnForbidden_WhenUserIsMemberOnly()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var actionItemId = Guid.NewGuid();

        context.Projects.Add(new Project
        {
            Id = projectId, Name = "P", Description = "D",
            StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31),
            CreatedByUserId = Guid.NewGuid() // Not creator
        });
        context.ProjectMembers.Add(new ProjectMember
        {
            Id = Guid.NewGuid(), ProjectId = projectId, UserId = userId, Role = UserRole.Member // Member role cannot delete
        });
        context.Categories.Add(new Category { Id = Guid.NewGuid(), ProjectId = projectId, Name = "Cat" });
        context.ActionItems.Add(new ActionItem
        {
            Id = actionItemId, ProjectId = projectId, CategoryId = Guid.NewGuid(),
            ActionItemName = "Item To Delete", Sequence = 1
        });
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);
        userContext.IsSystemAdmin.Returns(false);
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var handler = new DeleteActionItemCommandHandler(context, unitOfWork, userContext);
        var command = new DeleteActionItemCommand(projectId, actionItemId);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ActionItemErrors.Forbidden);
    }

    [Fact]
    public async Task Handle_Should_DeleteActionItem_WhenUserIsTeamLead()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var actionItemId = Guid.NewGuid();

        context.Projects.Add(new Project
        {
            Id = projectId, Name = "P", Description = "D",
            StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31),
            CreatedByUserId = Guid.NewGuid()
        });
        context.ProjectMembers.Add(new ProjectMember
        {
            Id = Guid.NewGuid(), ProjectId = projectId, UserId = userId, Role = UserRole.TeamLeader // TeamLeader can delete
        });
        context.Categories.Add(new Category { Id = Guid.NewGuid(), ProjectId = projectId, Name = "Cat" });
        context.ActionItems.Add(new ActionItem
        {
            Id = actionItemId, ProjectId = projectId, CategoryId = Guid.NewGuid(),
            ActionItemName = "Item To Delete", Sequence = 1
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

        var handler = new DeleteActionItemCommandHandler(context, unitOfWork, userContext);
        var command = new DeleteActionItemCommand(projectId, actionItemId);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());

        ActionItem? deletedItem = await context.ActionItems.SingleOrDefaultAsync(a => a.Id == actionItemId);
        deletedItem.Should().BeNull();

        PlannedSchedule? deletedSchedule = await context.PlannedSchedules.SingleOrDefaultAsync(s => s.ActionItemId == actionItemId);
        deletedSchedule.Should().BeNull();
    }
}
