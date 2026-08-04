using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Projects.CreateProject;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.Infrastructure.Interceptors;
using PMS.SharedKernel;
using Xunit;

namespace PMS.UnitTests.Projects;

public class CreateProjectCommandHandlerTests
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
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(false);
        userContext.UserId.Returns((Guid?)null);

        await using var context = CreateDbContext(userContext);
        var handler = new CreateProjectCommandHandler(context, unitOfWork, userContext);

        var command = new CreateProjectCommand(
            Name: "Unauthenticated Project",
            Description: "Test description",
            StartDate: DateOnly.FromDateTime(DateTime.Today),
            EndDate: DateOnly.FromDateTime(DateTime.Today.AddDays(10))
        );

        // Act
        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.Unauthorized);
    }

    [Fact]
    public async Task Handle_Should_ReturnConflict_WhenProjectNameAlreadyExists()
    {
        // Arrange
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(Guid.NewGuid());

        await using var context = CreateDbContext(userContext);
        var existingProject = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Existing Project",
            CreatedByUserId = Guid.NewGuid(),
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10))
        };
        context.Projects.Add(existingProject);
        await context.SaveChangesAsync();

        var handler = new CreateProjectCommandHandler(context, unitOfWork, userContext);

        var command = new CreateProjectCommand(
            Name: "Existing Project",
            Description: "Test description",
            StartDate: DateOnly.FromDateTime(DateTime.Today),
            EndDate: DateOnly.FromDateTime(DateTime.Today.AddDays(10))
        );

        // Act
        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProjectErrors.NameAlreadyExists("Existing Project"));
    }

    [Fact]
    public async Task Handle_Should_CreateProjectWithAuthenticatedUserIdFromAuditInterceptor_WhenValid()
    {
        // Arrange
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var authenticatedUserId = Guid.NewGuid();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(authenticatedUserId);

        await using var context = CreateDbContext(userContext);
        var handler = new CreateProjectCommandHandler(context, unitOfWork, userContext);

        var command = new CreateProjectCommand(
            Name: "New Super Project",
            Description: "A great project description",
            StartDate: DateOnly.FromDateTime(DateTime.Today),
            EndDate: DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            WeekStartDay: 1,
            DefaultTimelineScale: TimelineScale.Weekly,
            ProgressMode: ProgressMode.CountBased
        );

        // Act
        Result<Guid> result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());

        Project? createdProject = await context.Projects.SingleOrDefaultAsync(p => p.Id == result.Value);
        createdProject.Should().NotBeNull();
        createdProject!.Name.Should().Be("New Super Project");
        createdProject.Description.Should().Be("A great project description");
        createdProject.CreatedByUserId.Should().Be(authenticatedUserId);
        createdProject.DomainEvents.Should().ContainSingle(e => e is ProjectCreatedDomainEvent);
    }
}
