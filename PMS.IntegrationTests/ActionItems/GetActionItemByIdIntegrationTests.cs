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
using PMS.Application.ActionItems.GetActionItemById;
using PMS.Domain.ActionItems;
using PMS.Domain.ActualExecutions;
using PMS.Domain.Categories;
using PMS.Domain.PlannedSchedules;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.Infrastructure.Interceptors;
using Xunit;

namespace PMS.IntegrationTests.ActionItems;

public class GetActionItemByIdIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GetActionItemByIdIntegrationTests(WebApplicationFactory<Program> factory)
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
                    DbContextOptions<ApplicationDbContext> optionsWithInterceptor =
                        new DbContextOptionsBuilder<ApplicationDbContext>(options)
                            .AddInterceptors(interceptor)
                            .Options;

                    return new ApplicationDbContext(optionsWithInterceptor);
                });
            });
        });
    }

    [Fact]
    public async Task GetActionItemById_Should_Return401Unauthorized_WhenNotAuthenticated()
    {
        // Arrange
        HttpClient client = _factory.CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync(
            $"api/projects/{Guid.NewGuid()}/action-items/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetActionItemById_Should_Return404NotFound_WhenProjectDoesNotExist()
    {
        // Arrange
        (_, HttpClient client) = await CreateAuthenticatedClientAsync();

        // Act
        HttpResponseMessage response = await client.GetAsync(
            $"api/projects/{Guid.NewGuid()}/action-items/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetActionItemById_Should_Return404NotFound_WhenActionItemDoesNotExist()
    {
        // Arrange
        (User user, HttpClient client) = await CreateAuthenticatedClientAsync();
        (Guid projectId, _) = await SeedActionItemAsync(user.Id);

        // Act
        HttpResponseMessage response = await client.GetAsync(
            $"api/projects/{projectId}/action-items/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetActionItemById_Should_ReturnProjectedDetailsAndComputedStatus_WhenValid()
    {
        // Arrange
        (User user, HttpClient client) = await CreateAuthenticatedClientAsync();
        (Guid projectId, Guid actionItemId) = await SeedActionItemAsync(user.Id, includeDetails: true);

        // Act
        HttpResponseMessage response = await client.GetAsync(
            $"api/projects/{projectId}/action-items/{actionItemId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ActionItemResponse? item = await response.Content.ReadFromJsonAsync<ActionItemResponse>();
        item.Should().NotBeNull();
        item!.Id.Should().Be(actionItemId);
        item.ActionItemName.Should().Be("Integration Action Item");
        item.CategoryName.Should().Be("Integration Category");
        item.Priority.Should().Be((int)Priority.High);
        item.OwnerName.Should().Be("Integration Owner");
        item.Sequence.Should().Be(4);
        item.PlannedSchedule.Should().NotBeNull();
        item.PlannedSchedule!.PlannedEndDate.Should().Be(new DateOnly(2026, 1, 10));
        item.ActualExecution.Should().NotBeNull();
        item.ActualExecution!.ActualEndDate.Should().Be(new DateOnly(2026, 1, 9));
        item.ComputedStatus.Should().Be((int)ActionItemStatus.CompletedEarly);
        item.ComputedStatusLabel.Should().Be(nameof(ActionItemStatus.CompletedEarly));
        item.Weight.Should().Be(40m);
        item.Remarks.Should().Be("Integration remarks");
    }

    [Fact]
    public async Task GetActionItemById_Should_ReturnNullOptionalRelationships_WhenTheyDoNotExist()
    {
        // Arrange
        (User user, HttpClient client) = await CreateAuthenticatedClientAsync();
        (Guid projectId, Guid actionItemId) = await SeedActionItemAsync(user.Id);

        // Act
        HttpResponseMessage response = await client.GetAsync(
            $"api/projects/{projectId}/action-items/{actionItemId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ActionItemResponse? item = await response.Content.ReadFromJsonAsync<ActionItemResponse>();
        item.Should().NotBeNull();
        item!.SubCategoryId.Should().BeNull();
        item.SubCategoryName.Should().BeNull();
        item.PlannedSchedule.Should().BeNull();
        item.ActualExecution.Should().BeNull();
        item.ComputedStatus.Should().Be((int)ActionItemStatus.Plan);
    }

    private async Task<(User User, HttpClient Client)> CreateAuthenticatedClientAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tokenProvider = scope.ServiceProvider.GetRequiredService<ITokenProvider>();

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "ActionItem",
            LastName = "DetailsUser",
            Email = $"actionitem_details_{Guid.NewGuid()}@test.com",
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

    private async Task<(Guid ProjectId, Guid ActionItemId)> SeedActionItemAsync(
        Guid userId,
        bool includeDetails = false)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Guid projectId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();
        Guid actionItemId = Guid.NewGuid();

        context.Projects.Add(new Project
        {
            Id = projectId,
            Name = "Integration Action Item Project",
            Description = "Description",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31),
            CreatedByUserId = userId
        });
        context.ProjectMembers.Add(new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            Role = UserRole.Member
        });
        context.Categories.Add(new Category
        {
            Id = categoryId,
            ProjectId = projectId,
            Name = "Integration Category"
        });
        context.ActionItems.Add(new ActionItem
        {
            Id = actionItemId,
            ProjectId = projectId,
            CategoryId = categoryId,
            ActionItemName = "Integration Action Item",
            Priority = Priority.High,
            OwnerName = "Integration Owner",
            Sequence = 4,
            Weight = 40m,
            Remarks = "Integration remarks"
        });

        if (includeDetails)
        {
            context.PlannedSchedules.Add(new PlannedSchedule
            {
                Id = Guid.NewGuid(),
                ActionItemId = actionItemId,
                PlannedStartDate = new DateOnly(2026, 1, 1),
                PlannedEndDate = new DateOnly(2026, 1, 10),
                PlannedStartWeek = "WW01",
                PlannedEndWeek = "WW02",
                DurationCalendarDays = 10,
                DurationWorkingDays = 8
            });
            context.ActualExecutions.Add(new ActualExecution
            {
                Id = Guid.NewGuid(),
                ActionItemId = actionItemId,
                ActualStartDate = new DateOnly(2026, 1, 2),
                ActualEndDate = new DateOnly(2026, 1, 9),
                ActualHours = 20m,
                DelayReason = null
            });
        }

        await context.SaveChangesAsync();
        return (projectId, actionItemId);
    }
}
