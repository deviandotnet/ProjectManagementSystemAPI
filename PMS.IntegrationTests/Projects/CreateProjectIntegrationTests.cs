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
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.Infrastructure.Interceptors;
using PMS.SharedKernel;
using Xunit;

namespace PMS.IntegrationTests.Projects;

public class CreateProjectIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CreateProjectIntegrationTests(WebApplicationFactory<Program> factory)
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

    private async Task<(Guid UserId, HttpClient Client)> CreateAuthenticatedClientAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tokenProvider = scope.ServiceProvider.GetRequiredService<ITokenProvider>();

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
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

        return (user.Id, client);
    }

    [Fact]
    public async Task CreateProject_Should_Return401Unauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var client = _factory.CreateClient(); // Unauthenticated client
        var request = new CreateProject.ProjectRequest(
            Name: "Unauthenticated Project Request",
            Description: "Integration test description",
            StartDate: DateOnly.FromDateTime(DateTime.Today),
            EndDate: DateOnly.FromDateTime(DateTime.Today.AddDays(30))
        );

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("api/projects", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProject_Should_Return201Created_WhenAuthenticated()
    {
        // Arrange
        var (userId, client) = await CreateAuthenticatedClientAsync();
        var request = new CreateProject.ProjectRequest(
            Name: $"Integration Test Project {Guid.NewGuid()}",
            Description: "Integration test description",
            StartDate: DateOnly.FromDateTime(DateTime.Today),
            EndDate: DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            WeekStartDay: 1,
            DefaultTimelineScale: TimelineScale.Weekly,
            ProgressMode: ProgressMode.CountBased
        );

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("api/projects", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Guid createdId = await response.Content.ReadFromJsonAsync<Guid>();
        createdId.Should().NotBeEmpty();
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateProject_Should_Return409Conflict_WhenNameAlreadyExists()
    {
        // Arrange
        var (userId, client) = await CreateAuthenticatedClientAsync();
        string projectName = $"Duplicate Project {Guid.NewGuid()}";
        var request = new CreateProject.ProjectRequest(
            Name: projectName,
            Description: "First creation",
            StartDate: DateOnly.FromDateTime(DateTime.Today),
            EndDate: DateOnly.FromDateTime(DateTime.Today.AddDays(30))
        );

        HttpResponseMessage firstResponse = await client.PostAsJsonAsync("api/projects", request);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act
        HttpResponseMessage duplicateResponse = await client.PostAsJsonAsync("api/projects", request);

        // Assert
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
