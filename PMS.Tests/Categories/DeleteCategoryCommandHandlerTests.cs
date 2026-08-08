using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Categories.DeleteCategory;
using PMS.Domain.Categories;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.SharedKernel;
using Xunit;

namespace PMS.UnitTests.Categories;

public class DeleteCategoryCommandHandlerTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_Should_AllowMember_ToDeleteOwnCategory()
    {
        // Arrange
        await using var context = CreateDbContext();
        Guid userId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        var category = new Category
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = "My Category",
            CreatedByUserId = userId
        };
        var member = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            Role = UserRole.Member
        };
        context.Categories.Add(category);
        context.ProjectMembers.Add(member);
        await context.SaveChangesAsync();

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);
        userContext.IsSystemAdmin.Returns(false);

        var handler = new DeleteCategoryCommandHandler(context, unitOfWork, userContext);
        var command = new DeleteCategoryCommand(projectId, category.Id);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        (await context.Categories.AnyAsync(c => c.Id == category.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_Should_ReturnForbidden_WhenMemberTriesToDeleteAnotherMembersCategory()
    {
        // Arrange
        await using var context = CreateDbContext();
        Guid creatorUserId = Guid.NewGuid();
        Guid otherUserId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        var category = new Category
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = "Protected Category",
            CreatedByUserId = creatorUserId
        };
        var member = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = otherUserId,
            Role = UserRole.Member
        };
        context.Categories.Add(category);
        context.ProjectMembers.Add(member);
        await context.SaveChangesAsync();

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(otherUserId);
        userContext.IsSystemAdmin.Returns(false);

        var handler = new DeleteCategoryCommandHandler(context, unitOfWork, userContext);
        var command = new DeleteCategoryCommand(projectId, category.Id);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CategoryErrors.Forbidden);
    }
}
