using Bogus;
using FluentAssertions;
using NSubstitute;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Data;
using PMS.Application.Features.ProjectFeatures;
using PMS.Application.Features.ProjectFeatures.CreateProject;
using PMS.Domain.Abstractions.Errors;
using PMS.Domain.Entities;
using PMS.Domain.Enums;
using PMS.Infrastructure.Data;
using PMS.IntegrationTests.Helpers;

namespace PMS.IntegrationTests.ProjectFeatures;

public class CreateProjectHandlerTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IRepository<Project> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CreateProjectHandler _handler;
    private readonly Faker _faker = new();

    public CreateProjectHandlerTests()
    {
        _dbContext = TestDbContextFactory.Create();
        _repository = Substitute.For<IRepository<Project>>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new CreateProjectHandler(_dbContext, _repository, _unitOfWork);
    }

    [Fact]
    public async Task HandleAsync_WithValidRequest_ShouldCreateProjectAndReturnSuccess()
    {
        // Arrange
        var projectName = _faker.Commerce.ProductName();
        var description = _faker.Lorem.Sentence();
        var startDate = new DateOnly(2024, 1, 1);
        var endDate = new DateOnly(2024, 12, 31);
        var weekStartDay = _faker.Random.Int(0, 6);
        var createdByUserId = Guid.NewGuid();

        var command = new CreateProjectRequest(
            Name: projectName,
            Description: description,
            StartDate: startDate,
            EndDate: endDate,
            WeekStartDay: weekStartDay,
            DefaultTimelineScale: TimelineScale.Daily,
            CreatedByUserId: createdByUserId
        );

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var response = result.Value;
        response.Id.Should().NotBeEmpty();
        response.Name.Should().Be(projectName);
        response.Description.Should().Be(description);
        response.StartDate.Should().Be(startDate);
        response.EndDate.Should().Be(endDate);
        response.WeekStartDay.Should().Be(weekStartDay);
        response.DefaultTimelineScale.Should().Be(TimelineScale.Daily);
        response.Status.Should().Be(ProjectStatus.Active);
        response.ProgressMode.Should().Be(ProgressMode.CountBased);

        await _repository.Received(1).AddAsync(
            Arg.Is<Project>(p => p.Name == projectName && p.Description == description),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithPaddedNameAndDescription_ShouldTrimWhitespace()
    {
        // Arrange
        var rawName = _faker.Commerce.ProductName();
        var rawDescription = _faker.Lorem.Sentence();
        var paddedName = $"  {rawName}  ";
        var paddedDescription = $"  {rawDescription}  ";

        var command = new CreateProjectRequest(
            Name: paddedName,
            Description: paddedDescription,
            StartDate: new DateOnly(2024, 1, 1),
            EndDate: new DateOnly(2024, 12, 31),
            WeekStartDay: 1,
            DefaultTimelineScale: TimelineScale.Weekly,
            CreatedByUserId: Guid.NewGuid()
        );

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(rawName);
        result.Value.Description.Should().Be(rawDescription);

        await _repository.Received(1).AddAsync(
            Arg.Is<Project>(p => p.Name == rawName && p.Description == rawDescription),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithNullDescription_ShouldCreateProjectWithNullDescription()
    {
        // Arrange
        var projectName = _faker.Commerce.ProductName();

        var command = new CreateProjectRequest(
            Name: projectName,
            Description: null,
            StartDate: new DateOnly(2024, 1, 1),
            EndDate: new DateOnly(2024, 12, 31),
            WeekStartDay: 1,
            DefaultTimelineScale: TimelineScale.Daily,
            CreatedByUserId: Guid.NewGuid()
        );

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Should().BeNull();

        await _repository.Received(1).AddAsync(
            Arg.Is<Project>(p => p.Name == projectName && p.Description == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldSetDefaultStatusAndProgressMode()
    {
        // Arrange
        var command = new CreateProjectRequest(
            Name: _faker.Commerce.ProductName(),
            Description: _faker.Lorem.Sentence(),
            StartDate: new DateOnly(2024, 1, 1),
            EndDate: new DateOnly(2024, 12, 31),
            WeekStartDay: 1,
            DefaultTimelineScale: TimelineScale.Monthly,
            CreatedByUserId: Guid.NewGuid()
        );

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(ProjectStatus.Active);
        result.Value.ProgressMode.Should().Be(ProgressMode.CountBased);

        await _repository.Received(1).AddAsync(
            Arg.Is<Project>(p => p.Status == ProjectStatus.Active && p.ProgressMode == ProgressMode.CountBased),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WhenNameAlreadyExists_ShouldReturnConflictError()
    {
        // Arrange
        var existingName = _faker.Commerce.ProductName();
        _dbContext.Projects.Add(new Project
        {
            Id = Guid.NewGuid(),
            Name = existingName,
            Description = _faker.Lorem.Sentence(),
            StartDate = new DateOnly(2023, 1, 1),
            EndDate = new DateOnly(2023, 12, 31),
            WeekStartDay = 1,
            DefaultTimelineScale = TimelineScale.Daily,
            Status = ProjectStatus.Active,
            ProgressMode = ProgressMode.CountBased,
            CreatedByUserId = Guid.NewGuid()
        });
        await _dbContext.SaveChangesAsync();

        var command = new CreateProjectRequest(
            Name: existingName.ToUpperInvariant(), // case-insensitive duplicate
            Description: _faker.Lorem.Sentence(),
            StartDate: new DateOnly(2024, 1, 1),
            EndDate: new DateOnly(2024, 12, 31),
            WeekStartDay: 1,
            DefaultTimelineScale: TimelineScale.Daily,
            CreatedByUserId: Guid.NewGuid()
        );

        // Act
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Code.Should().Be("Project.NameAlreadyExists");
        result.Error.Type.Should().Be(ErrorType.Conflict);

        await _repository.DidNotReceive().AddAsync(Arg.Any<Project>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}
