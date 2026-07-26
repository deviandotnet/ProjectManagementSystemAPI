using FluentAssertions;
using PMS.Application.Features.ProjectFeatures.GetAllProjects;
using PMS.Domain.Entities;
using PMS.Domain.Enums;
using PMS.Infrastructure.Data;
using PMS.UnitTests.Helpers;
using Xunit;

namespace PMS.UnitTests.ProjectFeatures;

public class GetAllProjectsHandlerTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly GetAllProjectsHandler _handler;

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
        _dbContext.Projects.AddRange(
            new Project
            {
                Id = Guid.NewGuid(),
                Name = "Project Alpha",
                Description = "First project description",
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
                Name = "Project Beta",
                Description = "Second project description",
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

        var alpha = result.Value.Single(p => p.Name == "Project Alpha");
        alpha.Description.Should().Be("First project description");
        alpha.DefaultTimelineScale.Should().Be("Daily");
        alpha.ProgressMode.Should().Be("CountBased");
        alpha.Status.Should().Be("Active");

        var beta = result.Value.Single(p => p.Name == "Project Beta");
        beta.Description.Should().Be("Second project description");
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
