using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Categories.UpdateCategory;
using PMS.Domain.Categories;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.SharedKernel;
using Xunit;

namespace PMS.UnitTests.Categories;

public class UpdateCategoryCommandHandlerTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_Should_AllowMember_ToUpdateOwnCategory()
    {
        // Arrange
        await using var context = CreateDbContext();
        Guid userId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        var category = new Category
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = "Original Name",
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

        var handler = new UpdateCategoryCommandHandler(context, unitOfWork, userContext);
        var command = new UpdateCategoryCommand(projectId, category.Id, "Updated Name", 2, "#FF0000");

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        category.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task Handle_Should_ReturnForbidden_WhenMemberTriesToUpdateAnotherMembersCategory()
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
            Name = "Creator Category",
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

        var handler = new UpdateCategoryCommandHandler(context, unitOfWork, userContext);
        var command = new UpdateCategoryCommand(projectId, category.Id, "Hacked Name", 2, "#FF0000");

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CategoryErrors.Forbidden);
    }

    [Fact]
    public async Task Handle_Should_AllowProjectAdmin_ToUpdateAnotherMembersCategory()
    {
        // Arrange
        await using var context = CreateDbContext();
        Guid creatorUserId = Guid.NewGuid();
        Guid adminUserId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        var category = new Category
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = "Creator Category",
            CreatedByUserId = creatorUserId
        };
        var adminMember = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = adminUserId,
            Role = UserRole.Admin
        };
        context.Categories.Add(category);
        context.ProjectMembers.Add(adminMember);
        await context.SaveChangesAsync();

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(adminUserId);
        userContext.IsSystemAdmin.Returns(false);

        var handler = new UpdateCategoryCommandHandler(context, unitOfWork, userContext);
        var command = new UpdateCategoryCommand(projectId, category.Id, "Admin Updated Name", 2, "#FF0000");

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        category.Name.Should().Be("Admin Updated Name");
    }
}
