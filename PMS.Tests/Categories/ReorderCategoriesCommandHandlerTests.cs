using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Categories.ReorderCategories;
using PMS.Domain.Categories;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.SharedKernel;
using Xunit;

namespace PMS.UnitTests.Categories;

public class ReorderCategoriesCommandHandlerTests
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

        var handler = new ReorderCategoriesCommandHandler(context, unitOfWork, userContext);
        var command = new ReorderCategoriesCommand(Guid.NewGuid(), []);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.Unauthorized);
    }

    [Fact]
    public async Task Handle_Should_ReorderCategories_WhenUserIsMember()
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
        var cat1 = new Category { Id = Guid.NewGuid(), ProjectId = project.Id, Name = "Cat 1", DisplayOrder = 1 };
        var cat2 = new Category { Id = Guid.NewGuid(), ProjectId = project.Id, Name = "Cat 2", DisplayOrder = 2 };

        var member = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = userId,
            Role = UserRole.Member
        };

        context.Projects.Add(project);
        context.Categories.AddRange(cat1, cat2);
        context.ProjectMembers.Add(member);
        await context.SaveChangesAsync();

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);

        var handler = new ReorderCategoriesCommandHandler(context, unitOfWork, userContext);
        var command = new ReorderCategoriesCommand(project.Id,
        [
            new ReorderCategoryItem(cat1.Id, 10),
            new ReorderCategoryItem(cat2.Id, 20)
        ]);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var updatedCat1 = await context.Categories.FindAsync(cat1.Id);
        var updatedCat2 = await context.Categories.FindAsync(cat2.Id);

        updatedCat1!.DisplayOrder.Should().Be(10);
        updatedCat2!.DisplayOrder.Should().Be(20);
    }
}
