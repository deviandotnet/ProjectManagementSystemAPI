using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Projects.GetProjectProgress;
using PMS.Domain.ActionItems;
using PMS.Domain.ActualExecutions;
using PMS.Domain.PlannedSchedules;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.SharedKernel;
using Xunit;

namespace PMS.UnitTests.Projects;

public class GetProjectProgressQueryHandlerTests
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

        var handler = new GetProjectProgressQueryHandler(context, userContext, dateTimeProvider);
        var query = new GetProjectProgressQuery(Guid.NewGuid());

        // Act
        Result<ProjectProgressResponse> result = await handler.Handle(query, CancellationToken.None);

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
        var handler = new GetProjectProgressQueryHandler(context, userContext, dateTimeProvider);
        var query = new GetProjectProgressQuery(nonExistentProjectId);

        // Act
        Result<ProjectProgressResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProjectErrors.NotFound(nonExistentProjectId));
    }

    [Fact]
    public async Task Handle_Should_CalculateProgressCorrectly_CountBased()
    {
        // Arrange
        await using var context = CreateDbContext();
        Guid userId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        var project = new Project
        {
            Id = projectId,
            Name = "Count Project",
            ProgressMode = ProgressMode.CountBased,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 6, 30),
            CreatedByUserId = userId
        };
        var member = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            Role = UserRole.ProjectManager
        };

        // Action Item 1: Completed
        var ai1 = new ActionItem { Id = Guid.NewGuid(), ProjectId = projectId, CategoryId = Guid.NewGuid(), ActionItemName = "AI 1", Sequence = 1 };
        var ps1 = new PlannedSchedule { Id = Guid.NewGuid(), ActionItemId = ai1.Id, PlannedStartDate = new DateOnly(2026, 1, 1), PlannedEndDate = new DateOnly(2026, 1, 10) };
        var ae1 = new ActualExecution { Id = Guid.NewGuid(), ActionItemId = ai1.Id, ActualStartDate = new DateOnly(2026, 1, 1), ActualEndDate = new DateOnly(2026, 1, 9) };

        // Action Item 2: Ongoing
        var ai2 = new ActionItem { Id = Guid.NewGuid(), ProjectId = projectId, CategoryId = Guid.NewGuid(), ActionItemName = "AI 2", Sequence = 2 };
        var ps2 = new PlannedSchedule { Id = Guid.NewGuid(), ActionItemId = ai2.Id, PlannedStartDate = new DateOnly(2026, 1, 11), PlannedEndDate = new DateOnly(2026, 1, 20) };
        var ae2 = new ActualExecution { Id = Guid.NewGuid(), ActionItemId = ai2.Id, ActualStartDate = new DateOnly(2026, 1, 11) };

        // Action Item 3: Delayed
        var ai3 = new ActionItem { Id = Guid.NewGuid(), ProjectId = projectId, CategoryId = Guid.NewGuid(), ActionItemName = "AI 3", Sequence = 3 };
        var ps3 = new PlannedSchedule { Id = Guid.NewGuid(), ActionItemId = ai3.Id, PlannedStartDate = new DateOnly(2026, 1, 1), PlannedEndDate = new DateOnly(2026, 1, 5) };

        // Action Item 4: Plan
        var ai4 = new ActionItem { Id = Guid.NewGuid(), ProjectId = projectId, CategoryId = Guid.NewGuid(), ActionItemName = "AI 4", Sequence = 4 };
        var ps4 = new PlannedSchedule { Id = Guid.NewGuid(), ActionItemId = ai4.Id, PlannedStartDate = new DateOnly(2026, 2, 1), PlannedEndDate = new DateOnly(2026, 2, 10) };

        context.Projects.Add(project);
        context.ProjectMembers.Add(member);
        context.ActionItems.AddRange(ai1, ai2, ai3, ai4);
        context.PlannedSchedules.AddRange(ps1, ps2, ps3, ps4);
        context.ActualExecutions.AddRange(ae1, ae2);
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);

        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(new DateTime(2026, 1, 15));

        var handler = new GetProjectProgressQueryHandler(context, userContext, dateTimeProvider);
        var query = new GetProjectProgressQuery(projectId);

        // Act
        Result<ProjectProgressResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalActionItems.Should().Be(4);
        result.Value.CompletedActionItems.Should().Be(1);
        result.Value.OngoingActionItems.Should().Be(1);
        result.Value.DelayedActionItems.Should().Be(1);
        result.Value.PlannedActionItems.Should().Be(1);
        result.Value.ProgressPercent.Should().Be(25.0); // 1 / 4 * 100
    }

    [Fact]
    public async Task Handle_Should_CalculateProgressCorrectly_WeightBased()
    {
        // Arrange
        await using var context = CreateDbContext();
        Guid userId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        var project = new Project
        {
            Id = projectId,
            Name = "Weight Project",
            ProgressMode = ProgressMode.WeightBased,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 6, 30),
            CreatedByUserId = userId
        };
        var member = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            Role = UserRole.Member
        };

        // Item 1: Weight 30, Completed
        var ai1 = new ActionItem { Id = Guid.NewGuid(), ProjectId = projectId, CategoryId = Guid.NewGuid(), ActionItemName = "AI 1", Sequence = 1, Weight = 30 };
        var ps1 = new PlannedSchedule { Id = Guid.NewGuid(), ActionItemId = ai1.Id, PlannedStartDate = new DateOnly(2026, 1, 1), PlannedEndDate = new DateOnly(2026, 1, 10) };
        var ae1 = new ActualExecution { Id = Guid.NewGuid(), ActionItemId = ai1.Id, ActualStartDate = new DateOnly(2026, 1, 1), ActualEndDate = new DateOnly(2026, 1, 10) };

        // Item 2: Weight 70, Ongoing
        var ai2 = new ActionItem { Id = Guid.NewGuid(), ProjectId = projectId, CategoryId = Guid.NewGuid(), ActionItemName = "AI 2", Sequence = 2, Weight = 70 };
        var ps2 = new PlannedSchedule { Id = Guid.NewGuid(), ActionItemId = ai2.Id, PlannedStartDate = new DateOnly(2026, 1, 11), PlannedEndDate = new DateOnly(2026, 1, 30) };
        var ae2 = new ActualExecution { Id = Guid.NewGuid(), ActionItemId = ai2.Id, ActualStartDate = new DateOnly(2026, 1, 11) };

        context.Projects.Add(project);
        context.ProjectMembers.Add(member);
        context.ActionItems.AddRange(ai1, ai2);
        context.PlannedSchedules.AddRange(ps1, ps2);
        context.ActualExecutions.Add(ae1);
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);

        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(new DateTime(2026, 1, 15));

        var handler = new GetProjectProgressQueryHandler(context, userContext, dateTimeProvider);
        var query = new GetProjectProgressQuery(projectId);

        // Act
        Result<ProjectProgressResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalActionItems.Should().Be(2);
        result.Value.CompletedActionItems.Should().Be(1);
        result.Value.TotalWeight.Should().Be(100.0);
        result.Value.CompletedWeight.Should().Be(30.0);
        result.Value.ProgressPercent.Should().Be(30.0); // 30 / 100 * 100
    }
}
