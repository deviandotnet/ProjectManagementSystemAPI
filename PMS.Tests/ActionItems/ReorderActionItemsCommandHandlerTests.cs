using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.ActionItems.ReorderActionItems;
using PMS.Domain.ActionItems;
using PMS.Domain.Categories;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.SharedKernel;
using Xunit;

namespace PMS.UnitTests.ActionItems;

public class ReorderActionItemsCommandHandlerTests
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
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(false);

        var handler = new ReorderActionItemsCommandHandler(context, unitOfWork, userContext);
        var command = new ReorderActionItemsCommand(Guid.NewGuid(), []);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.Unauthorized);
    }

    [Fact]
    public async Task Handle_Should_ReorderActionItems_WhenUserIsMember()
    {
        // Arrange
        await using var context = CreateDbContext();
        Guid userId = Guid.NewGuid();
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Project",
            Description = "Desc",
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            CreatedByUserId = userId
        };
        var category = new Category { Id = Guid.NewGuid(), ProjectId = project.Id, Name = "Cat" };
        var item1 = new ActionItem { Id = Guid.NewGuid(), ProjectId = project.Id, CategoryId = category.Id, ActionItemName = "Item 1", Sequence = 1 };
        var item2 = new ActionItem { Id = Guid.NewGuid(), ProjectId = project.Id, CategoryId = category.Id, ActionItemName = "Item 2", Sequence = 2 };

        var member = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = userId,
            Role = UserRole.Member
        };

        context.Projects.Add(project);
        context.Categories.Add(category);
        context.ActionItems.AddRange(item1, item2);
        context.ProjectMembers.Add(member);
        await context.SaveChangesAsync();

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);

        var handler = new ReorderActionItemsCommandHandler(context, unitOfWork, userContext);
        var command = new ReorderActionItemsCommand(project.Id,
        [
            new ReorderActionItemItem(item1.Id, 5),
            new ReorderActionItemItem(item2.Id, 15)
        ]);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var updatedItem1 = await context.ActionItems.FindAsync(item1.Id);
        var updatedItem2 = await context.ActionItems.FindAsync(item2.Id);

        updatedItem1!.Sequence.Should().Be(5);
        updatedItem2!.Sequence.Should().Be(15);
    }
}
