using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PMS.API;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Projects.GetProjectsByUserId;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.Infrastructure.Interceptors;
using Xunit;

namespace PMS.IntegrationTests.Projects;

public class GetProjectsByUserIdIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GetProjectsByUserIdIntegrationTests(WebApplicationFactory<Program> factory)
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
            SystemRole = SystemRole.Admin
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        string token = tokenProvider.CreateAccessToken(user);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return (user, client);
    }

    [Fact]
    public async Task GetProjectsByUserId_Should_Return401Unauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var client = _factory.CreateClient();
        Guid randomUserId = Guid.NewGuid();

        // Act
        HttpResponseMessage response = await client.GetAsync($"api/users/{randomUserId}/projects");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProjectsByUserId_Should_Return404NotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var (_, client) = await CreateAuthenticatedClientAsync();
        Guid nonExistentUserId = Guid.NewGuid();

        // Act
        HttpResponseMessage response = await client.GetAsync($"api/users/{nonExistentUserId}/projects");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProjectsByUserId_Should_Return200OkWithProjects_WhenAuthenticated()
    {
        // Arrange
        var (user, client) = await CreateAuthenticatedClientAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var project1 = new Project
            {
                Id = Guid.NewGuid(),
                Name = "Integration Project 1",
                Description = "Description 1",
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
                CreatedByUserId = user.Id
            };
            context.Projects.Add(project1);
            await context.SaveChangesAsync();
        }

        // Act
        HttpResponseMessage response = await client.GetAsync($"api/users/{user.Id}/projects");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResponse<ProjectResponse>? projects =
            await response.Content.ReadFromJsonAsync<PagedResponse<ProjectResponse>>();
        projects.Should().NotBeNull();
        projects!.Items.Should().ContainSingle(p => p.Name == "Integration Project 1");
        projects.PageNumber.Should().Be(1);
        projects.PageSize.Should().Be(20);
        projects.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetCurrentUserProjects_Should_ReturnRequestedPageWithMetadata()
    {
        // Arrange
        var (user, client) = await CreateAuthenticatedClientAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            DateTimeOffset createdAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

            for (int index = 1; index <= 3; index++)
            {
                context.Projects.Add(new Project
                {
                    Id = Guid.Parse($"00000000-0000-0000-0000-{index:D12}"),
                    Name = $"Paged Project {index}",
                    StartDate = new DateOnly(2026, 1, 1),
                    EndDate = new DateOnly(2026, 12, 31),
                    CreatedByUserId = user.Id,
                    CreatedAt = createdAt.AddDays(index)
                });
            }

            await context.SaveChangesAsync();
        }

        // Act
        HttpResponseMessage response = await client.GetAsync("api/projects?pageNumber=2&pageSize=1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResponse<ProjectResponse>? page =
            await response.Content.ReadFromJsonAsync<PagedResponse<ProjectResponse>>();
        page.Should().NotBeNull();
        page!.Items.Should().ContainSingle();
        page.Items.Single().Id.Should().Be(Guid.Parse("00000000-0000-0000-0000-000000000002"));
        page.PageNumber.Should().Be(2);
        page.PageSize.Should().Be(1);
        page.TotalCount.Should().Be(3);
        page.TotalPages.Should().Be(3);
        page.HasPreviousPage.Should().BeTrue();
        page.HasNextPage.Should().BeTrue();
    }
}
