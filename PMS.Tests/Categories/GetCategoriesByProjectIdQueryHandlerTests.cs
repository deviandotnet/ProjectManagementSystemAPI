using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Caching;
using PMS.Application.Categories.GetCategoriesByProjectId;
using PMS.Domain.Categories;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.SharedKernel;
using PMS.UnitTests.Caching;
using Xunit;

namespace PMS.UnitTests.Categories;

public class GetCategoriesByProjectIdQueryHandlerTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_Should_ReturnCategories_WhenUserIsMember()
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
        var member = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = userId,
            Role = UserRole.Member
        };
        var c1 = new Category { Id = Guid.NewGuid(), ProjectId = project.Id, Name = "Cat 1", DisplayOrder = 2 };
        var c2 = new Category { Id = Guid.NewGuid(), ProjectId = project.Id, Name = "Cat 2", DisplayOrder = 1 };
        context.Projects.Add(project);
        context.ProjectMembers.Add(member);
        context.Categories.AddRange(c1, c2);
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);
        userContext.IsSystemAdmin.Returns(false);

        var handler = new GetCategoriesByProjectIdQueryHandler(context, userContext);
        var query = new GetCategoriesByProjectIdQuery(project.Id);

        // Act
        Result<PagedResponse<CategoryResponse>> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.First().Name.Should().Be("Cat 2"); // DisplayOrder = 1
        result.Value.PageNumber.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
        result.Value.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_Should_ReturnRequestedCategoryPageInDisplayOrder()
    {
        // Arrange
        await using var context = CreateDbContext();
        Guid userId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        context.Projects.Add(new Project
        {
            Id = projectId,
            Name = "Paged Categories",
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
        for (int displayOrder = 1; displayOrder <= 3; displayOrder++)
        {
            context.Categories.Add(new Category
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Name = $"Category {displayOrder}",
                DisplayOrder = displayOrder
            });
        }

        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);
        var handler = new GetCategoriesByProjectIdQueryHandler(context, userContext);
        var query = new GetCategoriesByProjectIdQuery(projectId, PageNumber: 2, PageSize: 1);

        // Act
        Result<PagedResponse<CategoryResponse>> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().ContainSingle();
        result.Value.Items.Single().Name.Should().Be("Category 2");
        result.Value.PageNumber.Should().Be(2);
        result.Value.PageSize.Should().Be(1);
        result.Value.TotalCount.Should().Be(3);
        result.Value.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task Handle_Should_CacheWholePage_RequireAuthorizationOnHit_AndRefreshAfterProjectInvalidation()
    {
        await using var context = CreateDbContext();
        Guid userId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        context.Projects.Add(new Project
        {
            Id = projectId,
            Name = "Cached Categories",
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
        context.Categories.AddRange(
            new Category { Id = Guid.NewGuid(), ProjectId = projectId, Name = "First", DisplayOrder = 1 },
            new Category { Id = Guid.NewGuid(), ProjectId = projectId, Name = "Second", DisplayOrder = 2 });
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);
        var cache = new RecordingApplicationCache();
        var handler = new GetCategoriesByProjectIdQueryHandler(context, userContext, cache);
        var query = new GetCategoriesByProjectIdQuery(projectId, PageNumber: 1, PageSize: 1);

        Result<PagedResponse<CategoryResponse>> initial = await handler.Handle(query, CancellationToken.None);

        context.Categories.Add(new Category
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = "New First",
            DisplayOrder = 0
        });
        await context.SaveChangesAsync();

        Result<PagedResponse<CategoryResponse>> cached = await handler.Handle(query, CancellationToken.None);

        initial.Value.TotalCount.Should().Be(2);
        cached.Value.TotalCount.Should().Be(2);
        cached.Value.Items.Single().Name.Should().Be("First");
        cache.FactoryCalls.Should().Be(1);

        userContext.IsAuthenticated.Returns(false);
        Result<PagedResponse<CategoryResponse>> unauthorized = await handler.Handle(query, CancellationToken.None);
        unauthorized.IsFailure.Should().BeTrue();
        unauthorized.Error.Should().Be(UserErrors.Unauthorized);
        cache.FactoryCalls.Should().Be(1);

        userContext.IsAuthenticated.Returns(true);
        await CacheInvalidation.ProjectAsync(cache, projectId, CancellationToken.None);

        Result<PagedResponse<CategoryResponse>> refreshed = await handler.Handle(query, CancellationToken.None);

        refreshed.Value.TotalCount.Should().Be(3);
        refreshed.Value.Items.Single().Name.Should().Be("New First");
        cache.FactoryCalls.Should().Be(2);
    }
}
