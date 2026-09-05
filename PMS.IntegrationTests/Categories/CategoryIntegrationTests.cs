using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PMS.API;
using PMS.API.Endpoints.Categories;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Categories.GetCategoriesByProjectId;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.Infrastructure.Interceptors;
using Xunit;

namespace PMS.IntegrationTests.Categories;

public class CategoryIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CategoryIntegrationTests(WebApplicationFactory<Program> factory)
    {
        var dbName = Guid.NewGuid().ToString();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(dbName)
                    .Options;

                services.AddSingleton(options);
                services.AddScoped(sp =>
                {
                    var interceptor = sp.GetRequiredService<AuditInterceptor>();
                    var optionsWithInterceptor = new DbContextOptionsBuilder<ApplicationDbContext>(options)
                        .AddInterceptors(interceptor)
                        .Options;

                    return new ApplicationDbContext(optionsWithInterceptor);
                });
            });
        });
    }

    private async Task<(User User, HttpClient Client)> CreateAuthenticatedClientAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tokenProvider = scope.ServiceProvider.GetRequiredService<ITokenProvider>();

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Integration",
            LastName = "User",
            Email = $"user_{Guid.NewGuid()}@test.com",
            PasswordHash = "hashedpassword",
            SystemRole = SystemRole.User
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        string token = tokenProvider.CreateAccessToken(user);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return (user, client);
    }

    private async Task<Guid> CreateProjectWithMemberAsync(Guid userId, UserRole role = UserRole.Member)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Category Integration Project",
            Description = "Desc",
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            CreatedByUserId = userId
        };
        var member = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = userId,
            Role = role
        };

        context.Projects.Add(project);
        context.ProjectMembers.Add(member);
        await context.SaveChangesAsync();

        return project.Id;
    }

    [Fact]
    public async Task CreateCategory_Should_Return201Created_WhenUserIsMember()
    {
        // Arrange
        var (user, client) = await CreateAuthenticatedClientAsync();
        Guid projectId = await CreateProjectWithMemberAsync(user.Id);

        var request = new CreateCategory.CreateCategoryRequest("Architecture", 1, "#3A86FF");

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync($"api/projects/{projectId}/categories", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Guid categoryId = await response.Content.ReadFromJsonAsync<Guid>();
        categoryId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetCategories_Should_Return200OkWithList()
    {
        // Arrange
        var (user, client) = await CreateAuthenticatedClientAsync();
        Guid projectId = await CreateProjectWithMemberAsync(user.Id);

        var request = new CreateCategory.CreateCategoryRequest("Backend", 1, "#00FF00");
        await client.PostAsJsonAsync($"api/projects/{projectId}/categories", request);

        // Act
        HttpResponseMessage response = await client.GetAsync($"api/projects/{projectId}/categories");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await response.Content.ReadFromJsonAsync<PagedResponse<CategoryResponse>>();
        categories.Should().NotBeNull();
        categories!.Items.Should().ContainSingle(c => c.Name == "Backend");
        categories.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetCategories_Should_ReturnRequestedPageInDisplayOrder()
    {
        // Arrange
        var (user, client) = await CreateAuthenticatedClientAsync();
        Guid projectId = await CreateProjectWithMemberAsync(user.Id);

        for (int displayOrder = 1; displayOrder <= 3; displayOrder++)
        {
            await client.PostAsJsonAsync(
                $"api/projects/{projectId}/categories",
                new CreateCategory.CreateCategoryRequest(
                    $"Category {displayOrder}",
                    displayOrder,
                    null));
        }

        // Act
        HttpResponseMessage response = await client.GetAsync(
            $"api/projects/{projectId}/categories?pageNumber=2&pageSize=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResponse<CategoryResponse>? page =
            await response.Content.ReadFromJsonAsync<PagedResponse<CategoryResponse>>();
        page.Should().NotBeNull();
        page!.Items.Should().ContainSingle();
        page.Items.Single().Name.Should().Be("Category 2");
        page.PageNumber.Should().Be(2);
        page.PageSize.Should().Be(1);
        page.TotalCount.Should().Be(3);
        page.TotalPages.Should().Be(3);
    }
}
