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
using PMS.Application.ActionItems.GetActionItems;
using PMS.Domain.ActionItems;
using PMS.Domain.Categories;
using PMS.Domain.PlannedSchedules;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.Infrastructure.Interceptors;
using Xunit;

namespace PMS.IntegrationTests.ActionItems;

public class GetActionItemsIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GetActionItemsIntegrationTests(WebApplicationFactory<Program> factory)
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
            FirstName = "ActionItem",
            LastName = "IntegrationUser",
            Email = $"actionitem_{Guid.NewGuid()}@test.com",
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
    public async Task GetActionItems_Should_Return401Unauthorized_WhenNoTokenProvided()
    {
        // Arrange
        var client = _factory.CreateClient();
        Guid projectId = Guid.NewGuid();

        // Act
        HttpResponseMessage response = await client.GetAsync($"api/projects/{projectId}/action-items");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetActionItems_Should_Return200OkWithActionItems_WhenUserIsMember()
    {
        // Arrange
        var (user, client) = await CreateAuthenticatedClientAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var project = new Project
            {
                Id = Guid.NewGuid(),
                Name = "Action Item Project",
                Description = "Desc",
                StartDate = DateOnly.FromDateTime(DateTime.Today),
                EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
                CreatedByUserId = user.Id
            };
            var member = new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                UserId = user.Id,
                Role = UserRole.Member
            };
            var category = new Category
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                Name = "Core Planning",
                DisplayOrder = 1
            };
            var actionItem = new ActionItem
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                CategoryId = category.Id,
                ActionItemName = "Setup CI/CD Pipeline",
                Priority = Priority.High,
                Sequence = 1
            };
            var schedule = new PlannedSchedule
            {
                Id = Guid.NewGuid(),
                ActionItemId = actionItem.Id,
                PlannedStartDate = new DateOnly(2026, 1, 1),
                PlannedEndDate = new DateOnly(2026, 1, 15),
                PlannedStartWeek = "WW01",
                PlannedEndWeek = "WW03",
                DurationCalendarDays = 15,
                DurationWorkingDays = 10
            };

            context.Projects.Add(project);
            context.ProjectMembers.Add(member);
            context.Categories.Add(category);
            context.ActionItems.Add(actionItem);
            context.PlannedSchedules.Add(schedule);
            await context.SaveChangesAsync();

            // Act
            HttpResponseMessage response = await client.GetAsync($"api/projects/{project.Id}/action-items");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var items = await response.Content.ReadFromJsonAsync<List<ActionItemResponse>>();
            items.Should().NotBeNull();
            items.Should().ContainSingle(ai => ai.ActionItemName == "Setup CI/CD Pipeline");
            items!.First().CategoryName.Should().Be("Core Planning");
            items.First().PlannedSchedule.Should().NotBeNull();
            items.First().PlannedSchedule!.PlannedStartWeek.Should().Be("WW01");
        }
    }
}
