using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PMS.API;
using PMS.API.Endpoints.ActionItems;
using PMS.Application.Abstractions.Authentication;
using PMS.Domain.ActionItems;
using PMS.Domain.Categories;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.Infrastructure.Interceptors;
using Xunit;

namespace PMS.IntegrationTests.ActionItems;

public class CreateActionItemIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CreateActionItemIntegrationTests(WebApplicationFactory<Program> factory)
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
            FirstName = "ActionItem",
            LastName = "CreateUser",
            Email = $"createaction_{Guid.NewGuid()}@test.com",
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

    [Fact]
    public async Task CreateActionItem_Should_Return201Created_WhenUserIsMember()
    {
        // Arrange
        var (user, client) = await CreateAuthenticatedClientAsync();
        Guid projectId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var project = new Project
            {
                Id = projectId,
                Name = "Create Action Item Project",
                Description = "Desc",
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
                CreatedByUserId = user.Id
            };
            var member = new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                UserId = user.Id,
                Role = UserRole.Member
            };
            var category = new Category
            {
                Id = categoryId,
                ProjectId = projectId,
                Name = "Sprint 1",
                DisplayOrder = 1
            };

            context.Projects.Add(project);
            context.ProjectMembers.Add(member);
            context.Categories.Add(category);
            await context.SaveChangesAsync();
        }

        var request = new CreateActionItem.CreateActionItemRequest(
            categoryId,
            null,
            "Build Auth Module",
            "Implement JWT tokens",
            (int)Priority.High,
            "John Lead",
            user.Id,
            10.0m,
            1,
            "Initial task",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 15));

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync($"api/projects/{projectId}/action-items", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Guid actionItemId = await response.Content.ReadFromJsonAsync<Guid>();
        actionItemId.Should().NotBeEmpty();
    }
}
