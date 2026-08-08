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
using PMS.Application.Users.GetUserById;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.Infrastructure.Interceptors;
using Xunit;

namespace PMS.IntegrationTests.Users;

public class GetUserByIdIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GetUserByIdIntegrationTests(WebApplicationFactory<Program> factory)
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

    [Fact]
    public async Task GetUserById_Should_Return401Unauthorized_WhenNotAuthenticated()
    {
        // Arrange
        var client = _factory.CreateClient();
        Guid randomId = Guid.NewGuid();

        // Act
        HttpResponseMessage response = await client.GetAsync($"api/users/{randomId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUserById_Should_Return404NotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var (_, client) = await CreateAuthenticatedClientAsync();
        Guid nonExistentId = Guid.NewGuid();

        // Act
        HttpResponseMessage response = await client.GetAsync($"api/users/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetUserById_Should_Return200Ok_WhenUserExists()
    {
        // Arrange
        var (authUser, client) = await CreateAuthenticatedClientAsync();

        Guid targetUserId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var targetUser = new User
            {
                Id = targetUserId,
                FirstName = "Jane",
                MiddleName = "Marie",
                LastName = "Doe",
                Email = $"jane_{Guid.NewGuid()}@test.com",
                PasswordHash = "secret_hash",
                SystemRole = SystemRole.User,
                IsActive = true
            };
            context.Users.Add(targetUser);
            await context.SaveChangesAsync();
        }

        // Act
        HttpResponseMessage response = await client.GetAsync($"api/users/{targetUserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        UserResponse? userResponse = await response.Content.ReadFromJsonAsync<UserResponse>();
        userResponse.Should().NotBeNull();
        userResponse!.Id.Should().Be(targetUserId);
        userResponse.FirstName.Should().Be("Jane");
        userResponse.MiddleName.Should().Be("Marie");
        userResponse.LastName.Should().Be("Doe");
        userResponse.SystemRole.Should().Be(SystemRole.User);
        userResponse.IsActive.Should().BeTrue();
    }
}
