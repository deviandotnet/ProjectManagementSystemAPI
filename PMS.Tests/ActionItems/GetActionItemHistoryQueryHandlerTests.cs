using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.ActionItems.GetActionItemHistory;
using PMS.Domain.ActionItems;
using PMS.Domain.AuditLogs;
using PMS.Domain.Categories;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.SharedKernel;
using Xunit;

namespace PMS.UnitTests.ActionItems;

public class GetActionItemHistoryQueryHandlerTests
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

        var nonExistentActionItemId = Guid.NewGuid();
        var handler = new GetActionItemHistoryQueryHandler(context, userContext);
        var query = new GetActionItemHistoryQuery(projectId, nonExistentActionItemId);

        // Act
        Result<PagedResponse<ActionItemHistoryResponse>> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ActionItemErrors.NotFound(nonExistentActionItemId));
    }

    [Fact]
    public async Task Handle_Should_ReturnAuditHistory_WhenValid()
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
            ActionItemName = "Audited Item", Sequence = 1
        });
        context.AuditLogs.Add(new AuditLog
        {
            EntityName = "ActionItem",
            EntityId = actionItemId.ToString(),
            Action = "Update",
            FieldName = "ActionItemName",
            OldValue = "Old Name",
            NewValue = "Audited Item",
            ChangedByUserId = userId,
            ChangedByName = "John Audit",
            ChangedAt = DateTimeOffset.UtcNow
        });
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);
        userContext.IsSystemAdmin.Returns(false);

        var handler = new GetActionItemHistoryQueryHandler(context, userContext);
        var query = new GetActionItemHistoryQuery(projectId, actionItemId);

        // Act
        Result<PagedResponse<ActionItemHistoryResponse>> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items.First().FieldName.Should().Be("ActionItemName");
        result.Value.Items.First().NewValue.Should().Be("Audited Item");
        result.Value.Items.First().ActivityMessage.Should().Contain("John Audit changed ActionItemName of 'Audited Item' from 'Old Name' to 'Audited Item'");
        result.Value.PageNumber.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
        result.Value.TotalCount.Should().Be(1);
        result.Value.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Should_ReturnRequestedHistoryPageWithMetadata()
    {
        // Arrange
        await using var context = CreateDbContext();
        Guid userId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();
        Guid actionItemId = Guid.NewGuid();

        context.Projects.Add(new Project
        {
            Id = projectId,
            Name = "History Project",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31),
            CreatedByUserId = userId
        });
        context.ProjectMembers.Add(new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            Role = UserRole.Member
        });
        context.Categories.Add(new Category
        {
            Id = categoryId,
            ProjectId = projectId,
            Name = "History Category"
        });
        context.ActionItems.Add(new ActionItem
        {
            Id = actionItemId,
            ProjectId = projectId,
            CategoryId = categoryId,
            ActionItemName = "History Item"
        });

        for (int index = 1; index <= 3; index++)
        {
            context.AuditLogs.Add(new AuditLog
            {
                Id = index,
                EntityName = "ActionItem",
                EntityId = actionItemId.ToString(),
                Action = "Update",
                FieldName = $"Field{index}",
                ChangedAt = new DateTimeOffset(2026, 1, index, 0, 0, 0, TimeSpan.Zero)
            });
        }

        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);
        var handler = new GetActionItemHistoryQueryHandler(context, userContext);
        var query = new GetActionItemHistoryQuery(
            projectId,
            actionItemId,
            PageNumber: 2,
            PageSize: 1);

        // Act
        Result<PagedResponse<ActionItemHistoryResponse>> result = await handler.Handle(
            query,
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle();
        result.Value.Items.Single().Id.Should().Be(2);
        result.Value.PageNumber.Should().Be(2);
        result.Value.PageSize.Should().Be(1);
        result.Value.TotalCount.Should().Be(3);
        result.Value.TotalPages.Should().Be(3);
        result.Value.HasPreviousPage.Should().BeTrue();
        result.Value.HasNextPage.Should().BeTrue();
    }
}
