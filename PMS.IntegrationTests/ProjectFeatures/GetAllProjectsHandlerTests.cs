using Bogus;
using FluentAssertions;
using PMS.Application.Features.ProjectFeatures.GetAllProjects;
using PMS.Domain.Entities;
using PMS.Domain.Enums;
using PMS.Infrastructure.Data;
using PMS.IntegrationTests.Helpers;

namespace PMS.IntegrationTests.ProjectFeatures;

public class GetAllProjectsHandlerTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly GetAllProjectsHandler _handler;
    private readonly Faker _faker = new();

    public GetAllProjectsHandlerTests()
    {
        _dbContext = TestDbContextFactory.Create();
        _handler = new GetAllProjectsHandler(_dbContext);
    }

    [Fact]
    public async Task HandleAsync_WhenProjectsExist_ShouldReturnSuccessResultWithListOfProjects()
    {
        // Arrange
        var createdByUserId = Guid.NewGuid();
        var nameAlpha = _faker.Commerce.ProductName();
        var descAlpha = _faker.Lorem.Sentence();
        var nameBeta = _faker.Commerce.ProductName();
        var descBeta = _faker.Lorem.Sentence();

        _dbContext.Projects.AddRange(
            new Project
            {
                Id = Guid.NewGuid(),
                Name = nameAlpha,
                Description = descAlpha,
                StartDate = new DateOnly(2025, 1, 1),
                EndDate = new DateOnly(2025, 6, 30),
                WeekStartDay = 1,
                DefaultTimelineScale = TimelineScale.Daily,
                ProgressMode = ProgressMode.CountBased,
                Status = ProjectStatus.Active,
                CreatedByUserId = createdByUserId,
                CreatedAt = DateTimeOffset.UtcNow
            },
            new Project
            {
                Id = Guid.NewGuid(),
                Name = nameBeta,
                Description = descBeta,
                StartDate = new DateOnly(2025, 2, 1),
                EndDate = new DateOnly(2025, 12, 31),
                WeekStartDay = 0,
                DefaultTimelineScale = TimelineScale.Weekly,
                ProgressMode = ProgressMode.WeightBased,
                Status = ProjectStatus.OnHold,
                CreatedByUserId = createdByUserId,
                CreatedAt = DateTimeOffset.UtcNow
            }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _handler.HandleAsync(new Unit(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCount(2);

        var alpha = result.Value.Single(p => p.Name == nameAlpha);
        alpha.Description.Should().Be(descAlpha);
        alpha.DefaultTimelineScale.Should().Be("Daily");
        alpha.ProgressMode.Should().Be("CountBased");
        alpha.Status.Should().Be("Active");

        var beta = result.Value.Single(p => p.Name == nameBeta);
        beta.Description.Should().Be(descBeta);
        beta.DefaultTimelineScale.Should().Be("Weekly");
        beta.ProgressMode.Should().Be("WeightBased");
        beta.Status.Should().Be("OnHold");
    }

    [Fact]
    public async Task HandleAsync_WhenNoProjectsExist_ShouldReturnSuccessResultWithEmptyList()
    {
        // Arrange & Act
        var result = await _handler.HandleAsync(new Unit(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }
}
