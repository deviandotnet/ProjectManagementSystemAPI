using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.SubCategories.CreateSubCategory;
using PMS.Domain.Categories;
using PMS.Domain.SubCategories;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.SharedKernel;
using Xunit;

namespace PMS.UnitTests.SubCategories;

public class CreateSubCategoryCommandHandlerTests
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

        var handler = new CreateSubCategoryCommandHandler(context, unitOfWork, userContext);
        var command = new CreateSubCategoryCommand(Guid.NewGuid(), "New Sub");

        // Act
        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.Unauthorized);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenCategoryDoesNotExist()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(Guid.NewGuid());
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var nonExistentCategoryId = Guid.NewGuid();
        var handler = new CreateSubCategoryCommandHandler(context, unitOfWork, userContext);
        var command = new CreateSubCategoryCommand(nonExistentCategoryId, "New Sub");

        // Act
        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CategoryErrors.NotFound(nonExistentCategoryId));
    }

    [Fact]
    public async Task Handle_Should_CreateSubCategory_WhenValid()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var category = new Category { Id = categoryId, ProjectId = Guid.NewGuid(), Name = "Category" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);
        userContext.IsSystemAdmin.Returns(true);
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var handler = new CreateSubCategoryCommandHandler(context, unitOfWork, userContext);
        var command = new CreateSubCategoryCommand(categoryId, "Created SubCategory", 1);

        // Act
        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());

        SubCategory? createdSub = await context.SubCategories.SingleOrDefaultAsync(sc => sc.Id == result.Value);
        createdSub.Should().NotBeNull();
        createdSub!.Name.Should().Be("Created SubCategory");
    }
}
