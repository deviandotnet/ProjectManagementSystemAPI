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
using PMS.Application.Dashboard.GetDashboard;
using PMS.Domain.ActionItems;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.Infrastructure.Interceptors;
using Xunit;

namespace PMS.IntegrationTests.Dashboard;

public class GetDashboardIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GetDashboardIntegrationTests(WebApplicationFactory<Program> factory)
    {
        string dbName = Guid.NewGuid().ToString();

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

    [Fact]
    public async Task GetDashboard_Should_Return401Unauthorized_WhenNoTokenProvided()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("api/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetDashboard_Should_PageProjectsAndKeepCompleteProjectKpis()
    {
        var (user, client) = await CreateAuthenticatedClientAsync();

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var projects = new[]
            {
                CreateProject("Alpha", user.Id),
                CreateProject("Beta", user.Id),
                CreateProject("Gamma", user.Id)
            };
            context.Projects.AddRange(projects);
            context.ProjectMembers.AddRange(projects.Select(project => new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                UserId = user.Id,
                Role = UserRole.Member
            }));

            for (int index = 1; index <= 2; index++)
            {
                context.ActionItems.Add(new ActionItem
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projects[1].Id,
                    CategoryId = Guid.NewGuid(),
                    ActionItemName = $"Beta Item {index}"
                });
            }

            await context.SaveChangesAsync();
        }

        HttpResponseMessage response = await client.GetAsync(
            "api/dashboard?pageNumber=2&pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        DashboardResponse? page = await response.Content.ReadFromJsonAsync<DashboardResponse>();
        page.Should().NotBeNull();
        page!.Projects.Should().ContainSingle();
        page.Projects.Single().ProjectName.Should().Be("Beta");
        page.Projects.Single().TotalActionItems.Should().Be(2);
        page.PageNumber.Should().Be(2);
        page.PageSize.Should().Be(1);
        page.TotalCount.Should().Be(3);
        page.TotalPages.Should().Be(3);
        page.HasPreviousPage.Should().BeTrue();
        page.HasNextPage.Should().BeTrue();
    }

    private static Project CreateProject(string name, Guid userId) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        StartDate = new DateOnly(2026, 1, 1),
        EndDate = new DateOnly(2026, 12, 31),
        CreatedByUserId = userId
    };

    private async Task<(User User, HttpClient Client)> CreateAuthenticatedClientAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tokenProvider = scope.ServiceProvider.GetRequiredService<ITokenProvider>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Dashboard",
            LastName = "User",
            Email = $"dashboard_{Guid.NewGuid()}@test.com",
            PasswordHash = "hashedpassword",
            SystemRole = SystemRole.User
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        string token = tokenProvider.CreateAccessToken(user);
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return (user, client);
    }
}
