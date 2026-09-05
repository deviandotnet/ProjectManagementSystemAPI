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
using PMS.Application.Projects.GetProjectAuditFeed;
using PMS.Domain.AuditLogs;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.Infrastructure.Interceptors;
using Xunit;

namespace PMS.IntegrationTests.Projects;

public class GetProjectAuditFeedIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GetProjectAuditFeedIntegrationTests(WebApplicationFactory<Program> factory)
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
    public async Task GetProjectAuditFeed_Should_Return401Unauthorized_WhenNoTokenProvided()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            $"api/projects/{Guid.NewGuid()}/audit");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProjectAuditFeed_Should_ReturnRequestedPageWithMetadata()
    {
        var (user, client) = await CreateAuthenticatedClientAsync();
        Guid projectId = Guid.NewGuid();

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Projects.Add(new Project
            {
                Id = projectId,
                Name = "Audited Project",
                StartDate = new DateOnly(2026, 1, 1),
                EndDate = new DateOnly(2026, 12, 31),
                CreatedByUserId = user.Id
            });
            context.ProjectMembers.Add(new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                UserId = user.Id,
                Role = UserRole.TeamLeader
            });
            await context.SaveChangesAsync();

            context.AuditLogs.RemoveRange(context.AuditLogs);
            await context.SaveChangesAsync();

            for (int index = 1; index <= 3; index++)
            {
                context.AuditLogs.Add(new AuditLog
                {
                    Id = index,
                    EntityName = "Project",
                    EntityId = projectId.ToString(),
                    Action = "Update",
                    ChangedAt = new DateTimeOffset(2026, 1, index, 0, 0, 0, TimeSpan.Zero)
                });
            }

            await context.SaveChangesAsync();
        }

        HttpResponseMessage response = await client.GetAsync(
            $"api/projects/{projectId}/audit?pageNumber=2&pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AuditFeedResponse? page = await response.Content.ReadFromJsonAsync<AuditFeedResponse>();
        page.Should().NotBeNull();
        page!.Feed.Should().ContainSingle();
        page.Feed.Single().Id.Should().Be(2);
        page.PageNumber.Should().Be(2);
        page.PageSize.Should().Be(1);
        page.TotalCount.Should().Be(3);
        page.TotalPages.Should().Be(3);
        page.HasPreviousPage.Should().BeTrue();
        page.HasNextPage.Should().BeTrue();
    }

    private async Task<(User User, HttpClient Client)> CreateAuthenticatedClientAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tokenProvider = scope.ServiceProvider.GetRequiredService<ITokenProvider>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Audit",
            LastName = "User",
            Email = $"audit_{Guid.NewGuid()}@test.com",
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
