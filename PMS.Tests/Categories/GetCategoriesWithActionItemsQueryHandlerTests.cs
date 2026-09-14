using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Caching;
using PMS.Application.Categories.GetCategoriesWithActionItems;
using PMS.Domain.ActionItems;
using PMS.Domain.ActualExecutions;
using PMS.Domain.Categories;
using PMS.Domain.PlannedSchedules;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.SubCategories;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.SharedKernel;
using PMS.UnitTests.Caching;
using Xunit;

namespace PMS.UnitTests.Categories;

public sealed class GetCategoriesWithActionItemsQueryHandlerTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
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
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var handler = new GetCategoriesWithActionItemsQueryHandler(
            context,
            userContext,
            dateTimeProvider);

        // Act
        Result<CategoriesWithActionItemsResponse> result = await handler.Handle(
            new GetCategoriesWithActionItemsQuery(Guid.NewGuid()),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.Unauthorized);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenProjectDoesNotExist()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(Guid.NewGuid());
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var handler = new GetCategoriesWithActionItemsQueryHandler(
            context,
            userContext,
            dateTimeProvider);
        Guid projectId = Guid.NewGuid();

        // Act
        Result<CategoriesWithActionItemsResponse> result = await handler.Handle(
            new GetCategoriesWithActionItemsQuery(projectId),
            CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProjectErrors.NotFound(projectId));
    }

    [Fact]
    public async Task Handle_Should_GroupFilteredActionItemsByCategory_WithLeafPagination()
    {
        // Arrange
        await using var context = CreateDbContext();
        (Guid userId, Guid projectId, Guid firstCategoryId, Guid secondCategoryId) =
            await SeedProjectAsync(context);

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(new DateTime(2026, 8, 20));
        var handler = new GetCategoriesWithActionItemsQueryHandler(
            context,
            userContext,
            dateTimeProvider);

        var query = new GetCategoriesWithActionItemsQuery(
            projectId,
            Priority: (int)Priority.High,
            PageNumber: 1,
            PageSize: 1);

        // Act
        Result<CategoriesWithActionItemsResponse> result =
            await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Categories.Should().ContainSingle();
        CategoryWithActionItemsResponse category = result.Value.Categories.Single();
        category.Id.Should().Be(firstCategoryId);
        category.ActionItems.Should().ContainSingle();
        CategorizedActionItemResponse actionItem = category.ActionItems.Single();
        actionItem.ActionItemName.Should().Be("Research interviews");
        actionItem.CategoryName.Should().Be("Discovery");
        actionItem.SubCategoryName.Should().Be("Research");
        actionItem.PlannedSchedule.Should().NotBeNull();
        actionItem.ComputedStatus.Should().Be((int)ActionItemStatus.Ongoing);
        result.Value.TotalCount.Should().Be(2);
        result.Value.TotalPages.Should().Be(2);
        result.Value.HasNextPage.Should().BeTrue();

        Result<CategoriesWithActionItemsResponse> secondPage = await handler.Handle(
            query with { PageNumber = 2 },
            CancellationToken.None);

        secondPage.Value.Categories.Should().ContainSingle();
        secondPage.Value.Categories.Single().Id.Should().Be(secondCategoryId);
        secondPage.Value.Categories.Single().ActionItems.Single().ActionItemName
            .Should().Be("Frontend implementation");
    }

    [Fact]
    public async Task Handle_Should_CacheFilteredPage_AndRefreshAfterProjectInvalidation()
    {
        // Arrange
        await using var context = CreateDbContext();
        (Guid userId, Guid projectId, _, _) = await SeedProjectAsync(context);
        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(new DateTime(2026, 8, 20));
        var cache = new RecordingApplicationCache();
        var handler = new GetCategoriesWithActionItemsQueryHandler(
            context,
            userContext,
            dateTimeProvider,
            cache);
        var query = new GetCategoriesWithActionItemsQuery(projectId);

        // Act
        Result<CategoriesWithActionItemsResponse> initial =
            await handler.Handle(query, CancellationToken.None);
        Result<CategoriesWithActionItemsResponse> cached =
            await handler.Handle(query, CancellationToken.None);

        await CacheInvalidation.ProjectAsync(cache, projectId, CancellationToken.None);

        Result<CategoriesWithActionItemsResponse> refreshed =
            await handler.Handle(query, CancellationToken.None);

        // Assert
        initial.IsSuccess.Should().BeTrue();
        cached.IsSuccess.Should().BeTrue();
        refreshed.IsSuccess.Should().BeTrue();
        cache.FactoryCalls.Should().Be(2);
    }

    private static async Task<(Guid UserId, Guid ProjectId, Guid FirstCategoryId, Guid SecondCategoryId)>
        SeedProjectAsync(ApplicationDbContext context)
    {
        Guid userId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid discoveryId = Guid.NewGuid();
        Guid developmentId = Guid.NewGuid();
        Guid researchId = Guid.NewGuid();

        context.Projects.Add(new Project
        {
            Id = projectId,
            Name = "Website Redesign",
            StartDate = new DateOnly(2026, 8, 1),
            EndDate = new DateOnly(2026, 10, 20),
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
            new Category
            {
                Id = discoveryId,
                ProjectId = projectId,
                Name = "Discovery",
                DisplayOrder = 1
            },
            new Category
            {
                Id = developmentId,
                ProjectId = projectId,
                Name = "Development",
                DisplayOrder = 2
            });
        context.SubCategories.Add(new SubCategory
        {
            Id = researchId,
            CategoryId = discoveryId,
            Name = "Research",
            DisplayOrder = 1
        });

        var researchItem = new ActionItem
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            CategoryId = discoveryId,
            SubCategoryId = researchId,
            ActionItemName = "Research interviews",
            Priority = Priority.High,
            Sequence = 1
        };
        var designItem = new ActionItem
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            CategoryId = discoveryId,
            ActionItemName = "Competitive review",
            Priority = Priority.Medium,
            Sequence = 2
        };
        var developmentItem = new ActionItem
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            CategoryId = developmentId,
            ActionItemName = "Frontend implementation",
            Priority = Priority.High,
            Sequence = 1
        };

        context.ActionItems.AddRange(researchItem, designItem, developmentItem);
        context.PlannedSchedules.Add(new PlannedSchedule
        {
            Id = Guid.NewGuid(),
            ActionItemId = researchItem.Id,
            PlannedStartDate = new DateOnly(2026, 8, 1),
            PlannedEndDate = new DateOnly(2026, 8, 30),
            PlannedStartWeek = "WW31",
            PlannedEndWeek = "WW35",
            DurationCalendarDays = 30,
            DurationWorkingDays = 20
        });
        context.ActualExecutions.Add(new ActualExecution
        {
            Id = Guid.NewGuid(),
            ActionItemId = researchItem.Id,
            ActualStartDate = new DateOnly(2026, 8, 2)
        });

        await context.SaveChangesAsync();

        return (userId, projectId, discoveryId, developmentId);
    }
}
