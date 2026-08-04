using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PMS.API;
using PMS.API.Endpoints.ProjectMembers;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.ProjectMembers.GetProjectMembers;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.Infrastructure.Interceptors;
using Xunit;

namespace PMS.IntegrationTests.Projects;

public class ProjectMembersIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProjectMembersIntegrationTests(WebApplicationFactory<Program> factory)
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
            FirstName = "Project",
            LastName = "Manager",
            Email = $"pm_{Guid.NewGuid()}@test.com",
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
    public async Task GetProjectMembers_Should_Return200OkWithMembers_WhenUserIsMember()
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
                Name = "Integration Project",
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

        // Act
        HttpResponseMessage response = await client.GetAsync($"api/projects/{projectId}/members");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        List<ProjectMemberResponse>? members = await response.Content.ReadFromJsonAsync<List<ProjectMemberResponse>>();
        members.Should().NotBeNull();
        members!.Should().ContainSingle(m => m.UserId == user.Id && m.Role == UserRole.ProjectManager);
    }

    [Fact]
    public async Task AddProjectMember_Should_Return201Created_WhenUserIsProjectManager()
    {
        // Arrange
        var (pmUser, client) = await CreateAuthenticatedClientAsync();
        Guid projectId = Guid.NewGuid();
        Guid newMemberUserId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var newMemberUser = new User
            {
                Id = newMemberUserId,
                FirstName = "Team",
                LastName = "Member",
                Email = $"member_{Guid.NewGuid()}@test.com",
                PasswordHash = "hashedpassword"
            };
            var project = new Project
            {
                Id = projectId,
                Name = "Project To Add Member",
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
                CreatedByUserId = pmUser.Id
            };
            var pmMember = new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                UserId = pmUser.Id,
                Role = UserRole.ProjectManager,
                JoinedAt = DateTimeOffset.UtcNow
            };

            context.Users.Add(newMemberUser);
            context.Projects.Add(project);
            context.ProjectMembers.Add(pmMember);
            await context.SaveChangesAsync();
        }

        var request = new AddProjectMember.AddMemberRequest(newMemberUserId, UserRole.TeamLeader);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync($"api/projects/{projectId}/members", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
