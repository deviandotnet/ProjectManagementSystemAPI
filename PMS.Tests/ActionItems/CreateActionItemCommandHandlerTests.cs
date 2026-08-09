using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.ActionItems.CreateActionItem;
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

public class CreateActionItemCommandHandlerTests
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
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var handler = new CreateActionItemCommandHandler(context, unitOfWork, userContext);
        var command = new CreateActionItemCommand(
            Guid.NewGuid(), Guid.NewGuid(), null, "Task", null,
            Priority.Medium, null, null, null, 1, null,
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 10));

        // Act
        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

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
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var nonExistentProjectId = Guid.NewGuid();
        var handler = new CreateActionItemCommandHandler(context, unitOfWork, userContext);
        var command = new CreateActionItemCommand(
            nonExistentProjectId, Guid.NewGuid(), null, "Task", null,
            Priority.Medium, null, null, null, 1, null,
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 10));

        // Act
        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProjectErrors.NotFound(nonExistentProjectId));
    }

    [Fact]
    public async Task Handle_Should_ReturnReadOnlyAccess_WhenUserIsViewer()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        context.Projects.Add(new Project
        {
            Id = projectId, Name = "P", Description = "D",
            StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31),
            CreatedByUserId = Guid.NewGuid()
        });
        context.ProjectMembers.Add(new ProjectMember
        {
            Id = Guid.NewGuid(), ProjectId = projectId, UserId = userId, Role = UserRole.Viewer
        });
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);
        userContext.IsSystemAdmin.Returns(false);
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var handler = new CreateActionItemCommandHandler(context, unitOfWork, userContext);
        var command = new CreateActionItemCommand(
            projectId, Guid.NewGuid(), null, "Task", null,
            Priority.Medium, null, null, null, 1, null,
            new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 10));

        // Act
        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ActionItemErrors.ReadOnlyAccess);
    }

    [Fact]
    public async Task Handle_Should_CreateActionItemAndPlannedSchedule_WhenValid()
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
        context.Categories.Add(new Category
        {
            Id = categoryId, ProjectId = projectId, Name = "Cat 1"
        });
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);
        userContext.IsSystemAdmin.Returns(false);
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var handler = new CreateActionItemCommandHandler(context, unitOfWork, userContext);
        var command = new CreateActionItemCommand(
            projectId, categoryId, null, "Setup Database", "Initialize EF Core schemas",
            Priority.High, "John Developer", userId, 15.5m, 1, "High Priority Task",
            new DateOnly(2026, 1, 5), new DateOnly(2026, 1, 16));

        // Act
        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());

        ActionItem? actionItem = await context.ActionItems.SingleOrDefaultAsync(a => a.Id == result.Value);
        actionItem.Should().NotBeNull();
        actionItem!.ActionItemName.Should().Be("Setup Database");
        actionItem.DomainEvents.Should().ContainSingle(e => e is ActionItemCreatedDomainEvent);

        PlannedSchedule? schedule = await context.PlannedSchedules.SingleOrDefaultAsync(s => s.ActionItemId == result.Value);
        schedule.Should().NotBeNull();
        schedule!.PlannedStartWeek.Should().Be("WW02");
        schedule.PlannedEndWeek.Should().Be("WW03");
        schedule.DurationCalendarDays.Should().Be(12);
        schedule.DurationWorkingDays.Should().Be(10); // 12 calendar days minus 2 weekend days (Jan 10, 11)
    }
}
