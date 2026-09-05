using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PMS.API;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.ActionItems.GetActionItemHistory;
using PMS.Domain.ActionItems;
using PMS.Domain.AuditLogs;
using PMS.Domain.Categories;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.Infrastructure.Interceptors;
using Xunit;

namespace PMS.IntegrationTests.ActionItems;

public class GetActionItemHistoryIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GetActionItemHistoryIntegrationTests(WebApplicationFactory<Program> factory)
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
    public async Task GetActionItemHistory_Should_Return401Unauthorized_WhenNoTokenProvided()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            $"api/projects/{Guid.NewGuid()}/action-items/{Guid.NewGuid()}/history");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("history")]
    [InlineData("audit")]
    public async Task GetActionItemHistory_Should_ReturnRequestedPageForBothAliases(string routeSuffix)
    {
        var (user, client) = await CreateAuthenticatedClientAsync();
        Guid projectId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();
        Guid actionItemId = Guid.NewGuid();

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Projects.Add(new Project
            {
                Id = projectId,
                Name = "History Project",
                StartDate = new DateOnly(2026, 1, 1),
                EndDate = new DateOnly(2026, 12, 31),
                CreatedByUserId = user.Id
            });
            context.ProjectMembers.Add(new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                UserId = user.Id,
                Role = UserRole.Member
            });
            context.Categories.Add(new Category
            {
                Id = categoryId,
                ProjectId = projectId,
                Name = "History Category"
            });
            context.ActionItems.Add(new ActionItem
            {
                Id = actionItemId,
                ProjectId = projectId,
                CategoryId = categoryId,
                ActionItemName = "Audited Item"
            });
            await context.SaveChangesAsync();

            context.AuditLogs.RemoveRange(context.AuditLogs);
            await context.SaveChangesAsync();

            for (int index = 1; index <= 3; index++)
            {
                context.AuditLogs.Add(new AuditLog
                {
                    Id = index,
                    EntityName = "ActionItem",
                    EntityId = actionItemId.ToString(),
                    Action = "Update",
                    FieldName = $"Field{index}",
                    ChangedAt = new DateTimeOffset(2026, 1, index, 0, 0, 0, TimeSpan.Zero)
                });
            }

            await context.SaveChangesAsync();
        }

        HttpResponseMessage response = await client.GetAsync(
            $"api/projects/{projectId}/action-items/{actionItemId}/{routeSuffix}?pageNumber=2&pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        PagedResponse<ActionItemHistoryResponse>? page =
            await response.Content.ReadFromJsonAsync<PagedResponse<ActionItemHistoryResponse>>();
        page.Should().NotBeNull();
        page!.Items.Should().ContainSingle();
        page.Items.Single().Id.Should().Be(2);
        page.PageNumber.Should().Be(2);
        page.PageSize.Should().Be(1);
        page.TotalCount.Should().Be(3);
        page.TotalPages.Should().Be(3);
    }

    private async Task<(User User, HttpClient Client)> CreateAuthenticatedClientAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tokenProvider = scope.ServiceProvider.GetRequiredService<ITokenProvider>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "History",
            LastName = "User",
            Email = $"history_{Guid.NewGuid()}@test.com",
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
