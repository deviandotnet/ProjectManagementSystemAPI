using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Projects.DeleteProject;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.Infrastructure.Interceptors;
using PMS.SharedKernel;
using Xunit;

namespace PMS.UnitTests.Projects;

public class DeleteProjectCommandHandlerTests
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
        var handler = new DeleteProjectCommandHandler(context, unitOfWork, userContext);
        var command = new DeleteProjectCommand(Guid.NewGuid());

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

        await using var context = CreateDbContext(userContext);
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new DeleteProjectCommandHandler(context, unitOfWork, userContext);
        var nonExistentId = Guid.NewGuid();
        var command = new DeleteProjectCommand(nonExistentId);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProjectErrors.NotFound(nonExistentId));
    }

    [Fact]
    public async Task Handle_Should_DeleteProjectAndRaiseDomainEvent_WhenProjectExists()
    {
        // Arrange
        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(Guid.NewGuid());

        await using var context = CreateDbContext(userContext);

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Project To Delete",
            Description = "Will be deleted",
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10))
        };
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new DeleteProjectCommandHandler(context, unitOfWork, userContext);
        var command = new DeleteProjectCommand(project.Id);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());

        Project? deletedProject = await context.Projects.SingleOrDefaultAsync(p => p.Id == project.Id);
        deletedProject.Should().BeNull();
        project.DomainEvents.Should().ContainSingle(e => e is ProjectDeletedDomainEvent);
    }
}
