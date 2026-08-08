using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PMS.API;
using PMS.API.Endpoints.Projects;
using PMS.Application.Abstractions.Authentication;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.Infrastructure.Interceptors;
using PMS.SharedKernel;
using Xunit;

namespace PMS.IntegrationTests.Projects;

public class UpdateProjectIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public UpdateProjectIntegrationTests(WebApplicationFactory<Program> factory)
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
            PasswordHash = "hashedpassword"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        string token = tokenProvider.CreateAccessToken(user);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return (user, client);
    }

    [Fact]
    public async Task UpdateProject_Should_Return401Unauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var client = _factory.CreateClient();
        Guid randomId = Guid.NewGuid();
        var request = new UpdateProject.UpdateProjectRequest(
            Name: "Updated Name",
            Description: "Desc",
            StartDate: DateOnly.FromDateTime(DateTime.Today),
            EndDate: DateOnly.FromDateTime(DateTime.Today.AddDays(10))
        );

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync($"api/projects/{randomId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProject_Should_Return404NotFound_WhenProjectDoesNotExist()
    {
        // Arrange
        var (_, client) = await CreateAuthenticatedClientAsync();
        Guid nonExistentId = Guid.NewGuid();
        var request = new UpdateProject.UpdateProjectRequest(
            Name: "Updated Name",
            Description: "Desc",
            StartDate: DateOnly.FromDateTime(DateTime.Today),
            EndDate: DateOnly.FromDateTime(DateTime.Today.AddDays(10))
        );

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync($"api/projects/{nonExistentId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateProject_Should_Return204NoContent_WhenUpdateIsSuccessful()
    {
        // Arrange
        var (user, client) = await CreateAuthenticatedClientAsync();
        Guid projectId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var project = new Project
            {
                Id = projectId,
                Name = "Original Project Name",
                Description = "Original Description",
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
                CreatedByUserId = user.Id
            };
            var member = new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                UserId = user.Id,
                Role = UserRole.ProjectManager,
                JoinedAt = DateTimeOffset.UtcNow
            };
            context.Projects.Add(project);
            context.ProjectMembers.Add(member);
            await context.SaveChangesAsync();
        }

        var request = new UpdateProject.UpdateProjectRequest(
            Name: "Updated Project Name",
            Description: "Updated Description",
            StartDate: DateOnly.FromDateTime(DateTime.Today),
            EndDate: DateOnly.FromDateTime(DateTime.Today.AddDays(20)),
            WeekStartDay: 1,
            DefaultTimelineScale: TimelineScale.Weekly,
            ProgressMode: ProgressMode.CountBased,
            Status: ProjectStatus.Active
        );

        // Act
        HttpResponseMessage response = await client.PutAsJsonAsync($"api/projects/{projectId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var updatedProject = await context.Projects.SingleOrDefaultAsync(p => p.Id == projectId);
            updatedProject.Should().NotBeNull();
            updatedProject!.Name.Should().Be("Updated Project Name");
            updatedProject.Description.Should().Be("Updated Description");
        }
    }
}
