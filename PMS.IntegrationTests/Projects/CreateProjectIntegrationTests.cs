using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PMS.API;
using PMS.API.Endpoints.Projects;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.SharedKernel;

namespace PMS.IntegrationTests.Projects;

public class CreateProjectIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
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
                services.AddScoped(sp => new ApplicationDbContext(options));
            });
        });

        _client = _factory.CreateClient();
    }

    private async Task<Guid> SeedUserAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "User",
            Email = $"user_{Guid.NewGuid()}@test.com",
            PasswordHash = "hashedpassword"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user.Id;
    }

    [Fact]
    public async Task CreateProject_Should_Return201Created_WhenRequestIsValid()
    {
        // Arrange
        Guid userId = await SeedUserAsync();
        var request = new CreateProject.ProjectRequest(
            Name: "Integration Test Project",
            Description: "Integration test description",
            StartDate: DateOnly.FromDateTime(DateTime.Today),
            EndDate: DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            WeekStartDay: 1,
            DefaultTimelineScale: TimelineScale.Weekly,
            ProgressMode: ProgressMode.CountBased,
            CreatedByUserId: userId
        );

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("api/projects", request);

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
        Guid userId = await SeedUserAsync();
        var request = new CreateProject.ProjectRequest(
            Name: "Duplicate Project",
            Description: "First creation",
            StartDate: DateOnly.FromDateTime(DateTime.Today),
            EndDate: DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            CreatedByUserId: userId
        );

        HttpResponseMessage firstResponse = await _client.PostAsJsonAsync("api/projects", request);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act
        HttpResponseMessage duplicateResponse = await _client.PostAsJsonAsync("api/projects", request);

        // Assert
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
