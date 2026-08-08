using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.SubCategories.UpdateSubCategory;
using PMS.Domain.Categories;
using PMS.Domain.SubCategories;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.SharedKernel;
using Xunit;

namespace PMS.UnitTests.SubCategories;

public class UpdateSubCategoryCommandHandlerTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenSubCategoryDoesNotExist()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(Guid.NewGuid());
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var nonExistentSubId = Guid.NewGuid();
        var handler = new UpdateSubCategoryCommandHandler(context, unitOfWork, userContext);
        var command = new UpdateSubCategoryCommand(Guid.NewGuid(), nonExistentSubId, "Updated Sub", 1);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SubCategoryErrors.NotFound(nonExistentSubId));
    }

    [Fact]
    public async Task Handle_Should_UpdateSubCategory_WhenValid()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var subCategoryId = Guid.NewGuid();

        var category = new Category { Id = categoryId, ProjectId = Guid.NewGuid(), Name = "Category" };
        var subCategory = new SubCategory { Id = subCategoryId, CategoryId = categoryId, Name = "Original Name", DisplayOrder = 0 };
        context.Categories.Add(category);
        context.SubCategories.Add(subCategory);
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);
        userContext.IsSystemAdmin.Returns(true);
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var handler = new UpdateSubCategoryCommandHandler(context, unitOfWork, userContext);
        var command = new UpdateSubCategoryCommand(categoryId, subCategoryId, "Updated Name", 5);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());

        SubCategory? updatedSub = await context.SubCategories.SingleOrDefaultAsync(sc => sc.Id == subCategoryId);
        updatedSub.Should().NotBeNull();
        updatedSub!.Name.Should().Be("Updated Name");
        updatedSub.DisplayOrder.Should().Be(5);
    }
}
