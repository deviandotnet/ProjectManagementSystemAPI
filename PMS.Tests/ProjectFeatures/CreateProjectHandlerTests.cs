using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Data;
using PMS.Application.Features.ProjectFeatures.CreateProject;
using PMS.Domain.Entities;
using PMS.Domain.Enums;
using PMS.Domain.Abstractions.Errors;
using Xunit;
using PMS.Infrastructure.Data;
using PMS.Application.Features.ProjectFeatures;
using PMS.UnitTests.Helpers;

namespace PMS.UnitTests.ProjectFeatures;

public class CreateProjectHandlerTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IRepository<Project> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CreateProjectHandler _handler;

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
        var command = new CreateProjectRequest(
            Name: "New Project",
            Description: "Sample description",
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
        var response = result.Value;
        response.Id.Should().NotBeEmpty();
        response.Name.Should().Be("New Project");
        response.Description.Should().Be("Sample description");
        response.StartDate.Should().Be(command.StartDate);
        response.EndDate.Should().Be(command.EndDate);
        response.WeekStartDay.Should().Be(1);
        response.DefaultTimelineScale.Should().Be(TimelineScale.Daily);
        response.Status.Should().Be(ProjectStatus.Active);
        response.ProgressMode.Should().Be(ProgressMode.CountBased);

        await _repository.Received(1).AddAsync(
            Arg.Is<Project>(p => p.Name == "New Project" && p.Description == "Sample description"),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithPaddedNameAndDescription_ShouldTrimWhitespace()
    {
        // Arrange
        var command = new CreateProjectRequest(
            Name: "  Untrimmed Project Name  ",
            Description: "  Untrimmed description  ",
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
        result.Value.Name.Should().Be("Untrimmed Project Name");
        result.Value.Description.Should().Be("Untrimmed description");

        await _repository.Received(1).AddAsync(
            Arg.Is<Project>(p => p.Name == "Untrimmed Project Name" && p.Description == "Untrimmed description"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_WithNullDescription_ShouldCreateProjectWithNullDescription()
    {
        // Arrange
        var command = new CreateProjectRequest(
            Name: "Project Without Description",
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
            Arg.Is<Project>(p => p.Name == "Project Without Description" && p.Description == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ShouldSetDefaultStatusAndProgressMode()
    {
        // Arrange
        var command = new CreateProjectRequest(
            Name: "Default Status Project",
            Description: "Testing default values",
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
        var existingName = "Existing Project";
        _dbContext.Projects.Add(new Project
        {
            Id = Guid.NewGuid(),
            Name = existingName,
            Description = "Old description",
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
            Name: existingName.ToUpperInvariant(), // case‑insensitive duplicate
            Description: "New description",
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
