using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PMS.API;
using PMS.API.Endpoints.Holidays;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Holidays.GetHolidays;
using PMS.Domain.HolidayCalendars;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.Infrastructure.Interceptors;
using Xunit;

namespace PMS.IntegrationTests.Holidays;

public class HolidayIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HolidayIntegrationTests(WebApplicationFactory<Program> factory)
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

    private async Task<(User User, HttpClient Client)> CreateAdminClientAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tokenProvider = scope.ServiceProvider.GetRequiredService<ITokenProvider>();

        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Admin",
            LastName = "User",
            Email = $"admin_{Guid.NewGuid()}@test.com",
            PasswordHash = "hashedpassword",
            SystemRole = SystemRole.Admin
        };
        context.Users.Add(adminUser);
        await context.SaveChangesAsync();

        string token = tokenProvider.CreateAccessToken(adminUser);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return (adminUser, client);
    }

    [Fact]
    public async Task GetHolidays_Should_Return200OkWithList()
    {
        // Arrange
        var (_, client) = await CreateAdminClientAsync();

        // Act
        HttpResponseMessage response = await client.GetAsync("api/holidays");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var holidays = await response.Content.ReadFromJsonAsync<List<HolidayResponse>>();
        holidays.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateHoliday_Should_Return201Created_WhenAdmin()
    {
        // Arrange
        var (_, client) = await CreateAdminClientAsync();
        var request = new CreateHoliday.CreateHolidayRequest(
            new DateOnly(2026, 8, 25),
            "National Heroes Day",
            (int)HolidayType.National,
            true);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("api/holidays", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Guid holidayId = await response.Content.ReadFromJsonAsync<Guid>();
        holidayId.Should().NotBeEmpty();
    }
}
