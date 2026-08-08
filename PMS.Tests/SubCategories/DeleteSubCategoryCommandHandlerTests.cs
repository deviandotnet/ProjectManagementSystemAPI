using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.SubCategories.DeleteSubCategory;
using PMS.Domain.Categories;
using PMS.Domain.SubCategories;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.SharedKernel;
using Xunit;

namespace PMS.UnitTests.SubCategories;

public class DeleteSubCategoryCommandHandlerTests
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
        var handler = new DeleteSubCategoryCommandHandler(context, unitOfWork, userContext);
        var command = new DeleteSubCategoryCommand(Guid.NewGuid(), nonExistentSubId);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(SubCategoryErrors.NotFound(nonExistentSubId));
    }

    [Fact]
    public async Task Handle_Should_DeleteSubCategory_WhenValid()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var subCategoryId = Guid.NewGuid();

        var category = new Category { Id = categoryId, ProjectId = Guid.NewGuid(), Name = "Category" };
        var subCategory = new SubCategory { Id = subCategoryId, CategoryId = categoryId, Name = "Sub To Delete" };
        context.Categories.Add(category);
        context.SubCategories.Add(subCategory);
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);
        userContext.IsSystemAdmin.Returns(true);
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var handler = new DeleteSubCategoryCommandHandler(context, unitOfWork, userContext);
        var command = new DeleteSubCategoryCommand(categoryId, subCategoryId);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());

        SubCategory? deletedSub = await context.SubCategories.SingleOrDefaultAsync(sc => sc.Id == subCategoryId);
        deletedSub.Should().BeNull();
    }
}
