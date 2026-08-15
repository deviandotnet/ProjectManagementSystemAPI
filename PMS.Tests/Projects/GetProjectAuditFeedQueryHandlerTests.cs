using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Projects.GetProjectAuditFeed;
using PMS.Domain.ActionItems;
using PMS.Domain.AuditLogs;
using PMS.Domain.Categories;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.SharedKernel;
using Xunit;

namespace PMS.UnitTests.Projects;

public class GetProjectAuditFeedQueryHandlerTests
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

        var handler = new GetProjectAuditFeedQueryHandler(context, userContext);
        var query = new GetProjectAuditFeedQuery(Guid.NewGuid());

        // Act
        Result<AuditFeedResponse> result = await handler.Handle(query, CancellationToken.None);

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

        var nonExistentId = Guid.NewGuid();
        var handler = new GetProjectAuditFeedQueryHandler(context, userContext);
        var query = new GetProjectAuditFeedQuery(nonExistentId);

        // Act
        Result<AuditFeedResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProjectErrors.NotFound(nonExistentId));
    }

    [Fact]
    public async Task Handle_Should_ReturnForbidden_WhenUserRoleIsBelowTeamLead()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        context.Projects.Add(new Project
        {
            Id = projectId, Name = "Alpha", Description = "Desc",
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

        var handler = new GetProjectAuditFeedQueryHandler(context, userContext);
        var query = new GetProjectAuditFeedQuery(projectId);

        // Act
        Result<AuditFeedResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProjectErrors.Forbidden);
    }

    [Fact]
    public async Task Handle_Should_ReturnFormattedAuditFeed_ForTeamLeaderOrHigher()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var actionItemId = Guid.NewGuid();

        context.Projects.Add(new Project
        {
            Id = projectId, Name = "Alpha Project", Description = "Desc",
            StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31),
            CreatedByUserId = userId
        });
        context.ProjectMembers.Add(new ProjectMember
        {
            Id = Guid.NewGuid(), ProjectId = projectId, UserId = userId, Role = UserRole.TeamLeader
        });
        context.Categories.Add(new Category { Id = categoryId, ProjectId = projectId, Name = "Architecture" });
        context.ActionItems.Add(new ActionItem
        {
            Id = actionItemId, ProjectId = projectId, CategoryId = categoryId,
            ActionItemName = "Design Data Model", Sequence = 1
        });

        context.AuditLogs.AddRange(
            new AuditLog
            {
                Id = 1,
                EntityName = "Project",
                EntityId = projectId.ToString(),
                Action = "Create",
                ChangedByName = "John Admin",
                ChangedAt = new DateTimeOffset(2026, 7, 14, 9, 30, 0, TimeSpan.Zero)
            },
            new AuditLog
            {
                Id = 2,
                EntityName = "ActionItem",
                EntityId = actionItemId.ToString(),
                Action = "Update",
                FieldName = "PlannedStartDate",
                OldValue = "2026-01-03",
                NewValue = "2026-01-10",
                ChangedByName = "John Dela Cruz",
                ChangedAt = new DateTimeOffset(2026, 7, 14, 9, 45, 0, TimeSpan.Zero)
            }
        );
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);
        userContext.IsSystemAdmin.Returns(false);

        var handler = new GetProjectAuditFeedQueryHandler(context, userContext);
        var query = new GetProjectAuditFeedQuery(projectId);

        // Act
        Result<AuditFeedResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ProjectId.Should().Be(projectId);
        result.Value.ProjectName.Should().Be("Alpha Project");
        result.Value.Feed.Should().HaveCount(2);

        var updateFeedItem = result.Value.Feed.First();
        updateFeedItem.EntityName.Should().Be("ActionItem");
        updateFeedItem.EntityTitle.Should().Be("Design Data Model");
        updateFeedItem.FieldName.Should().Be("PlannedStartDate");
        updateFeedItem.ActivityMessage.Should().Contain("John Dela Cruz changed PlannedStartDate of 'Design Data Model' from '2026-01-03' to '2026-01-10'");

        var createFeedItem = result.Value.Feed.Last();
        createFeedItem.EntityName.Should().Be("Project");
        createFeedItem.ActivityMessage.Should().Contain("John Admin created Project 'Alpha Project'");
    }
}
