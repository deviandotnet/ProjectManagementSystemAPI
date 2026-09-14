using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PMS.API;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Categories.GetCategoriesWithActionItems;
using PMS.Domain.ActionItems;
using PMS.Domain.Categories;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.SubCategories;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.Infrastructure.Interceptors;
using Xunit;

namespace PMS.IntegrationTests.Categories;

public sealed class GetCategoriesWithActionItemsIntegrationTests
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GetCategoriesWithActionItemsIntegrationTests(
        WebApplicationFactory<Program> factory)
    {
        string databaseName = Guid.NewGuid().ToString();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(databaseName)
                    .Options;

                services.AddSingleton(options);
                services.AddScoped(serviceProvider =>
                {
                    var interceptor =
                        serviceProvider.GetRequiredService<AuditInterceptor>();
                    var interceptedOptions =
                        new DbContextOptionsBuilder<ApplicationDbContext>(options)
                            .AddInterceptors(interceptor)
                            .Options;

                    return new ApplicationDbContext(interceptedOptions);
                });
            });
        });
    }

    [Fact]
    public async Task GetCategoriesWithActionItems_Should_ReturnUnauthorized_WhenTokenIsMissing()
    {
        // Arrange
        HttpClient client = _factory.CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync(
            $"api/projects/{Guid.NewGuid()}/categories/action-items");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCategoriesWithActionItems_Should_ReturnGroupedFilteredPage_WhenUserIsMember()
    {
        // Arrange
        (User user, HttpClient client) = await CreateAuthenticatedClientAsync();
        Guid projectId = Guid.NewGuid();
        Guid discoveryId = Guid.NewGuid();
        Guid developmentId = Guid.NewGuid();
        Guid researchId = Guid.NewGuid();

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Projects.Add(new Project
            {
                Id = projectId,
                Name = "Website Redesign",
                StartDate = new DateOnly(2026, 8, 1),
                EndDate = new DateOnly(2026, 10, 20),
                CreatedByUserId = user.Id
            });
            context.ProjectMembers.Add(new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                UserId = user.Id,
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
            context.ActionItems.AddRange(
                new ActionItem
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    CategoryId = discoveryId,
                    SubCategoryId = researchId,
                    ActionItemName = "Stakeholder interviews",
                    Priority = Priority.High,
                    Sequence = 1
                },
                new ActionItem
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    CategoryId = developmentId,
                    ActionItemName = "Frontend implementation",
                    Priority = Priority.Medium,
                    Sequence = 1
                });
            await context.SaveChangesAsync();
        }

        // Act
        HttpResponseMessage response = await client.GetAsync(
            $"api/projects/{projectId}/categories/action-items" +
            $"?categoryId={discoveryId}&pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        CategoriesWithActionItemsResponse? page =
            await response.Content.ReadFromJsonAsync<CategoriesWithActionItemsResponse>();

        page.Should().NotBeNull();
        page!.Categories.Should().ContainSingle();
        page.Categories.Single().Name.Should().Be("Discovery");
        page.Categories.Single().ActionItems.Should().ContainSingle();
        page.Categories.Single().ActionItems.Single().ActionItemName
            .Should().Be("Stakeholder interviews");
        page.Categories.Single().ActionItems.Single().SubCategoryName
            .Should().Be("Research");
        page.PageNumber.Should().Be(1);
        page.PageSize.Should().Be(10);
        page.TotalCount.Should().Be(1);
        page.TotalPages.Should().Be(1);
    }

    private async Task<(User User, HttpClient Client)> CreateAuthenticatedClientAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tokenProvider = scope.ServiceProvider.GetRequiredService<ITokenProvider>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Category",
            LastName = "Viewer",
            Email = $"category_{Guid.NewGuid()}@test.com",
            PasswordHash = "hashedpassword",
            SystemRole = SystemRole.User
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", tokenProvider.CreateAccessToken(user));

        return (user, client);
    }
}
