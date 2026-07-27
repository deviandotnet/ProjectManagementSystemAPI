using Bogus;
using FluentAssertions;
using PMS.Application.Features.ProjectFeatures;
using PMS.Application.Features.ProjectFeatures.GetProjectById;
using PMS.Domain.Entities;
using PMS.Domain.Enums;
using PMS.Infrastructure.Data;
using PMS.IntegrationTests.Helpers;

namespace PMS.IntegrationTests.ProjectFeatures;

public class GetProjectByIdHandlerTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly GetProjectByIdHandler _handler;
    private readonly Faker _faker = new();

    public GetProjectByIdHandlerTests()
    {
        _dbContext = TestDbContextFactory.Create();
        _handler = new GetProjectByIdHandler(_dbContext);
    }

    [Fact]
    public async Task HandleAsync_WhenProjectExists_ShouldReturnSuccessResultWithProject()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var createdByUserId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;
        var projectName = _faker.Commerce.ProductName();
        var projectDescription = _faker.Lorem.Sentence();
        var startDate = new DateOnly(2025, 3, 1);
        var endDate = new DateOnly(2025, 11, 30);

        var projectEntity = new Project
        {
            Id = projectId,
            Name = projectName,
            Description = projectDescription,
            StartDate = startDate,
            EndDate = endDate,
            WeekStartDay = 1,
            DefaultTimelineScale = TimelineScale.Monthly,
            ProgressMode = ProgressMode.CountBased,
            Status = ProjectStatus.Active,
            CreatedByUserId = createdByUserId,
            CreatedAt = createdAt,
            UpdatedAt = null
        };
        _dbContext.Projects.Add(projectEntity);
        await _dbContext.SaveChangesAsync();

        var request = new GetProjectByIdRequest(projectId);

        // Act
        var result = await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(projectId);
        result.Value.Name.Should().Be(projectName);
        result.Value.Description.Should().Be(projectDescription);
        result.Value.StartDate.Should().Be(startDate);
        result.Value.EndDate.Should().Be(endDate);
        result.Value.WeekStartDay.Should().Be(1);
        result.Value.DefaultTimelineScale.Should().Be("Monthly");
        result.Value.ProgressMode.Should().Be("CountBased");
        result.Value.Status.Should().Be("Active");
        result.Value.CreatedByUserId.Should().Be(createdByUserId);
        result.Value.CreatedAt.Should().Be(createdAt);
        result.Value.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenProjectDoesNotExist_ShouldReturnNotFoundError()
    {
        // Arrange
        var nonExistentProjectId = Guid.NewGuid();
        var request = new GetProjectByIdRequest(nonExistentProjectId);

        // Act
        var result = await _handler.HandleAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(ProjectErrors.NotFound(nonExistentProjectId));
        result.Error.Code.Should().Be("Project.NotFound");
        result.Error.Type.Should().Be(Domain.Abstractions.Errors.ErrorType.NotFound);
    }
}
