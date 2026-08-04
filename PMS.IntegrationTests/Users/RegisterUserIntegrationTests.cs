using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PMS.API;
using PMS.API.Endpoints.Users;
using PMS.Infrastructure.Database;

namespace PMS.IntegrationTests.Users;

public class RegisterUserIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public RegisterUserIntegrationTests(WebApplicationFactory<Program> factory)
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

    [Fact]
    public async Task RegisterUser_Should_Return201Created_WhenRequestIsValid()
    {
        // Arrange
        var request = new RegisterUser.RegisterUserRequest(
            FirstName: "Integration",
            MiddleName: null,
            LastName: "Tester",
            Email: "integration.tester@example.com",
            Password: "SecurePassword123!"
        );

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("api/users", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Guid createdId = await response.Content.ReadFromJsonAsync<Guid>();
        createdId.Should().NotBeEmpty();
        response.Headers.Location.Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterUser_Should_Return409Conflict_WhenEmailAlreadyExists()
    {
        // Arrange
        var request = new RegisterUser.RegisterUserRequest(
            FirstName: "Duplicate",
            MiddleName: null,
            LastName: "User",
            Email: "duplicate.user@example.com",
            Password: "SecurePassword123!"
        );

        HttpResponseMessage firstResponse = await _client.PostAsJsonAsync("api/users", request);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act
        HttpResponseMessage duplicateResponse = await _client.PostAsJsonAsync("api/users", request);

        // Assert
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
