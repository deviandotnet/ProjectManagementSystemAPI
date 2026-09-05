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
using PMS.Application.Projects.GetTimeline;
using PMS.Domain.ActionItems;
using PMS.Domain.Categories;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.SubCategories;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.Infrastructure.Interceptors;
using PMS.SharedKernel;
using Xunit;

namespace PMS.IntegrationTests.Projects;

public class GetTimelineIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GetTimelineIntegrationTests(WebApplicationFactory<Program> factory)
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
    public async Task GetTimeline_Should_Return401Unauthorized_WhenNoTokenProvided()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(
            $"api/projects/{Guid.NewGuid()}/timeline");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTimeline_Should_ReturnRequestedActionItemPageWithContextHeaders()
    {
        var (user, client) = await CreateAuthenticatedClientAsync();
        Guid projectId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();
        Guid subCategoryId = Guid.NewGuid();

        using (IServiceScope scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.Projects.Add(new Project
            {
                Id = projectId,
                Name = "Timeline Project",
                Description = "Description",
                StartDate = new DateOnly(2026, 1, 1),
                EndDate = new DateOnly(2026, 1, 31),
                WeekStartDay = 1,
                DefaultTimelineScale = TimelineScale.Weekly,
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
                Name = "Delivery",
                DisplayOrder = 1
            });
            context.SubCategories.Add(new SubCategory
            {
                Id = subCategoryId,
                CategoryId = categoryId,
                Name = "Backend",
                DisplayOrder = 1
            });

            for (int sequence = 1; sequence <= 3; sequence++)
            {
                context.ActionItems.Add(new ActionItem
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    CategoryId = categoryId,
                    SubCategoryId = subCategoryId,
                    ActionItemName = $"Timeline Item {sequence}",
                    Sequence = sequence
                });
            }

            await context.SaveChangesAsync();
        }

        HttpResponseMessage response = await client.GetAsync(
            $"api/projects/{projectId}/timeline?pageNumber=2&pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        TimelineResponse? timeline = await response.Content.ReadFromJsonAsync<TimelineResponse>();
        timeline.Should().NotBeNull();
        timeline!.Rows.Select(row => row.RowType)
            .Should().Equal("Category", "SubCategory", "ActionItem");
        timeline.Rows[^1].Label.Should().Be("Timeline Item 2");
        timeline.PageNumber.Should().Be(2);
        timeline.PageSize.Should().Be(1);
        timeline.TotalCount.Should().Be(3);
        timeline.TotalPages.Should().Be(3);
        timeline.HasPreviousPage.Should().BeTrue();
        timeline.HasNextPage.Should().BeTrue();
    }

    private async Task<(User User, HttpClient Client)> CreateAuthenticatedClientAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tokenProvider = scope.ServiceProvider.GetRequiredService<ITokenProvider>();
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Timeline",
            LastName = "User",
            Email = $"timeline_{Guid.NewGuid()}@test.com",
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
