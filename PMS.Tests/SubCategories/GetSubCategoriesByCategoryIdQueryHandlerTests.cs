using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.SubCategories.GetSubCategoriesByCategoryId;
using PMS.Domain.Categories;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.SubCategories;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.SharedKernel;
using Xunit;

namespace PMS.UnitTests.SubCategories;

public class GetSubCategoriesByCategoryIdQueryHandlerTests
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

        var handler = new GetSubCategoriesByCategoryIdQueryHandler(context, userContext);
        var query = new GetSubCategoriesByCategoryIdQuery(Guid.NewGuid());

        // Act
        Result<PagedResponse<SubCategoryResponse>> result = await handler.Handle(query, CancellationToken.None);

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

        var nonExistentCategoryId = Guid.NewGuid();
        var handler = new GetSubCategoriesByCategoryIdQueryHandler(context, userContext);
        var query = new GetSubCategoriesByCategoryIdQuery(nonExistentCategoryId);

        // Act
        Result<PagedResponse<SubCategoryResponse>> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CategoryErrors.NotFound(nonExistentCategoryId));
    }

    [Fact]
    public async Task Handle_Should_ReturnSubCategories_WhenCategoryExistsAndUserIsAuthorized()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var category = new Category
        {
            Id = categoryId,
            ProjectId = projectId,
            Name = "Parent Category"
        };
        var sub1 = new SubCategory { Id = Guid.NewGuid(), CategoryId = categoryId, Name = "Sub 1", DisplayOrder = 1 };
        var sub2 = new SubCategory { Id = Guid.NewGuid(), CategoryId = categoryId, Name = "Sub 2", DisplayOrder = 2 };
        context.Categories.Add(category);
        context.SubCategories.AddRange(sub1, sub2);
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);
        userContext.IsSystemAdmin.Returns(true);

        var handler = new GetSubCategoriesByCategoryIdQueryHandler(context, userContext);
        var query = new GetSubCategoriesByCategoryIdQuery(categoryId);

        // Act
        Result<PagedResponse<SubCategoryResponse>> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.Select(s => s.Name).Should().Contain(new[] { "Sub 1", "Sub 2" });
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_Should_ReturnRequestedSubCategoryPageInDisplayOrder()
    {
        // Arrange
        await using var context = CreateDbContext();
        Guid categoryId = Guid.NewGuid();
        context.Categories.Add(new Category
        {
            Id = categoryId,
            ProjectId = Guid.NewGuid(),
            Name = "Paged Parent"
        });
        for (int displayOrder = 1; displayOrder <= 3; displayOrder++)
        {
            context.SubCategories.Add(new SubCategory
            {
                Id = Guid.NewGuid(),
                CategoryId = categoryId,
                Name = $"Subcategory {displayOrder}",
                DisplayOrder = displayOrder
            });
        }

        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(Guid.NewGuid());
        userContext.IsSystemAdmin.Returns(true);
        var handler = new GetSubCategoriesByCategoryIdQueryHandler(context, userContext);
        var query = new GetSubCategoriesByCategoryIdQuery(categoryId, PageNumber: 2, PageSize: 1);

        // Act
        Result<PagedResponse<SubCategoryResponse>> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle();
        result.Value.Items.Single().Name.Should().Be("Subcategory 2");
        result.Value.PageNumber.Should().Be(2);
        result.Value.PageSize.Should().Be(1);
        result.Value.TotalCount.Should().Be(3);
        result.Value.TotalPages.Should().Be(3);
    }
}
