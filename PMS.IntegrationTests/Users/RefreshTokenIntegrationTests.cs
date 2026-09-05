using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PMS.API;
using PMS.API.Endpoints.Users;
using PMS.Application.Users;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using RefreshTokenEndpoint = PMS.API.Endpoints.Users.RefreshToken;
using RefreshTokenEntity = PMS.Domain.Users.RefreshToken;

namespace PMS.IntegrationTests.Users;

public class RefreshTokenIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public RefreshTokenIntegrationTests(WebApplicationFactory<Program> factory)
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
                services.AddScoped(serviceProvider => new ApplicationDbContext(options));
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task RefreshToken_Should_RotateTokenWithoutBearerAuthentication()
    {
        // Arrange
        AccessTokenResponse loginTokens = await RegisterAndLoginAsync();
        var request = new RefreshTokenEndpoint.RefreshTokenRequest(loginTokens.RefreshToken);

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("api/auth/refresh", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AccessTokenResponse? rotatedTokens = await response.Content.ReadFromJsonAsync<AccessTokenResponse>();
        rotatedTokens.Should().NotBeNull();
        rotatedTokens!.AccessToken.Should().NotBeNullOrWhiteSpace();
        rotatedTokens.RefreshToken.Should().NotBeNullOrWhiteSpace();
        rotatedTokens.RefreshToken.Should().NotBe(loginTokens.RefreshToken);

        HttpResponseMessage replayResponse = await _client.PostAsJsonAsync("api/auth/refresh", request);
        replayResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        HttpResponseMessage secondRotationResponse = await _client.PostAsJsonAsync(
            "api/auth/refresh",
            new RefreshTokenEndpoint.RefreshTokenRequest(rotatedTokens.RefreshToken));
        secondRotationResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        AccessTokenResponse? secondRotationTokens =
            await secondRotationResponse.Content.ReadFromJsonAsync<AccessTokenResponse>();
        secondRotationTokens.Should().NotBeNull();
        secondRotationTokens!.RefreshToken.Should().NotBe(rotatedTokens.RefreshToken);
    }

    [Fact]
    public async Task RefreshToken_Should_ReturnUnauthorized_WhenTokenDoesNotExist()
    {
        // Arrange
        var request = new RefreshTokenEndpoint.RefreshTokenRequest("unknown_refresh_token");

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("api/auth/refresh", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_Should_ReturnUnauthorizedAndDeleteToken_WhenTokenIsExpired()
    {
        // Arrange
        string expiredTokenValue = $"expired_{Guid.NewGuid():N}";

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = new User
            {
                Id = Guid.NewGuid(),
                FirstName = "Expired",
                LastName = "Token",
                Email = $"expired.{Guid.NewGuid():N}@example.com",
                PasswordHash = "hashed_password",
                IsActive = true
            };
            var expiredToken = new RefreshTokenEntity
            {
                Id = Guid.NewGuid(),
                Token = expiredTokenValue,
                UserId = user.Id,
                ExpiresOnUtc = DateTime.UtcNow.AddMinutes(-1)
            };

            context.AddRange(user, expiredToken);
            await context.SaveChangesAsync();
        }

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "api/auth/refresh",
            new RefreshTokenEndpoint.RefreshTokenRequest(expiredTokenValue));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using IServiceScope verificationScope = _factory.Services.CreateScope();
        ApplicationDbContext verificationContext =
            verificationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        (await verificationContext.RefreshTokens.AnyAsync(token => token.Token == expiredTokenValue))
            .Should().BeFalse();
    }

    [Fact]
    public async Task RefreshToken_Should_ReturnBadRequest_WhenTokenIsEmpty()
    {
        // Arrange
        var request = new RefreshTokenEndpoint.RefreshTokenRequest(string.Empty);

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("api/auth/refresh", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private async Task<AccessTokenResponse> RegisterAndLoginAsync()
    {
        string email = $"refresh.{Guid.NewGuid():N}@example.com";
        const string password = "SecurePassword123!";

        var registerRequest = new RegisterUser.RegisterUserRequest(
            FirstName: "Refresh",
            MiddleName: null,
            LastName: "Tester",
            Email: email,
            Password: password);

        HttpResponseMessage registerResponse = await _client.PostAsJsonAsync("api/users", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var loginRequest = new LoginUser.LoginUserRequest(email, password);
        HttpResponseMessage loginResponse = await _client.PostAsJsonAsync("api/users/login", loginRequest);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        AccessTokenResponse? tokens = await loginResponse.Content.ReadFromJsonAsync<AccessTokenResponse>();
        tokens.Should().NotBeNull();
        return tokens!;
    }
}
