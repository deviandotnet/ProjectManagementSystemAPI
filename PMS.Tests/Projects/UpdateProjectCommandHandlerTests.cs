using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Projects.UpdateProject;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.Infrastructure.Interceptors;
using PMS.SharedKernel;
using Xunit;

namespace PMS.UnitTests.Projects;

public class UpdateProjectCommandHandlerTests
{
    private static ApplicationDbContext CreateDbContext(IUserContext userContext)
    {
        var interceptor = new AuditInterceptor(userContext);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .AddInterceptors(interceptor)
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_Should_ReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(false);

        await using var context = CreateDbContext(userContext);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new UpdateProjectCommandHandler(context, unitOfWork, userContext);

        var command = new UpdateProjectCommand(
            Id: Guid.NewGuid(),
            Name: "Updated Name",
            Description: "Updated Description",
            StartDate: DateOnly.FromDateTime(DateTime.Today),
            EndDate: DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            WeekStartDay: 1,
            DefaultTimelineScale: TimelineScale.Weekly,
            ProgressMode: ProgressMode.CountBased,
            Status: ProjectStatus.Active
        );

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.Unauthorized);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenProjectDoesNotExist()
    {
        // Arrange
        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(Guid.NewGuid());
        userContext.IsSystemAdmin.Returns(true);

        await using var context = CreateDbContext(userContext);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new UpdateProjectCommandHandler(context, unitOfWork, userContext);
        var nonExistentId = Guid.NewGuid();

        var command = new UpdateProjectCommand(
            Id: nonExistentId,
            Name: "Updated Name",
            Description: "Updated Description",
            StartDate: DateOnly.FromDateTime(DateTime.Today),
            EndDate: DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            WeekStartDay: 1,
            DefaultTimelineScale: TimelineScale.Weekly,
            ProgressMode: ProgressMode.CountBased,
            Status: ProjectStatus.Active
        );

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProjectErrors.NotFound(nonExistentId));
    }

    [Fact]
    public async Task Handle_Should_ReturnConflict_WhenProjectNameAlreadyExistsOnAnotherProject()
    {
        // Arrange
        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(Guid.NewGuid());
        userContext.IsSystemAdmin.Returns(true);

        await using var context = CreateDbContext(userContext);

        var projectToUpdate = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Original Name",
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10))
        };

        var otherProject = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Conflict Name",
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10))
        };

        context.Projects.AddRange(projectToUpdate, otherProject);
        await context.SaveChangesAsync();

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new UpdateProjectCommandHandler(context, unitOfWork, userContext);

        var command = new UpdateProjectCommand(
            Id: projectToUpdate.Id,
            Name: "Conflict Name",
            Description: "Updated Description",
            StartDate: DateOnly.FromDateTime(DateTime.Today),
            EndDate: DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            WeekStartDay: 1,
            DefaultTimelineScale: TimelineScale.Weekly,
            ProgressMode: ProgressMode.CountBased,
            Status: ProjectStatus.Active
        );

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProjectErrors.NameAlreadyExists("Conflict Name"));
    }

    [Fact]
    public async Task Handle_Should_UpdateProjectAndRaiseDomainEvent_WhenValid()
    {
        // Arrange
        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(Guid.NewGuid());
        userContext.IsSystemAdmin.Returns(true);

        await using var context = CreateDbContext(userContext);

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Initial Project Name",
            Description = "Initial Description",
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10))
        };
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new UpdateProjectCommandHandler(context, unitOfWork, userContext);

        var command = new UpdateProjectCommand(
            Id: project.Id,
            Name: "Updated Project Name",
            Description: "Brand new description",
            StartDate: DateOnly.FromDateTime(DateTime.Today),
            EndDate: DateOnly.FromDateTime(DateTime.Today.AddDays(20)),
            WeekStartDay: 1,
            DefaultTimelineScale: TimelineScale.Monthly,
            ProgressMode: ProgressMode.WeightBased,
            Status: ProjectStatus.Active
        );

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());

        Project? updatedProject = await context.Projects.SingleOrDefaultAsync(p => p.Id == project.Id);
        updatedProject.Should().NotBeNull();
        updatedProject!.Name.Should().Be("Updated Project Name");
        updatedProject.Description.Should().Be("Brand new description");
        updatedProject.DefaultTimelineScale.Should().Be(TimelineScale.Monthly);
        updatedProject.ProgressMode.Should().Be(ProgressMode.WeightBased);
        updatedProject.DomainEvents.Should().ContainSingle(e => e is ProjectUpdatedDomainEvent);
    }
}
