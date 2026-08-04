using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.ProjectMembers.AddProjectMember;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.Infrastructure.Interceptors;
using PMS.SharedKernel;
using Xunit;

namespace PMS.UnitTests.Projects.ProjectMembers;

public class AddProjectMemberCommandHandlerTests
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

        var handler = new AddProjectMemberCommandHandler(context, unitOfWork, userContext);
        var command = new AddProjectMemberCommand(Guid.NewGuid(), Guid.NewGuid(), UserRole.Member);

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

        var nonExistentProjectId = Guid.NewGuid();
        var handler = new AddProjectMemberCommandHandler(context, unitOfWork, userContext);
        var command = new AddProjectMemberCommand(nonExistentProjectId, Guid.NewGuid(), UserRole.Member);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProjectErrors.NotFound(nonExistentProjectId));
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(Guid.NewGuid());
        await using var context = CreateDbContext(userContext);

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Test Project",
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            CreatedByUserId = userContext.UserId
        };
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var nonExistentUserId = Guid.NewGuid();
        var handler = new AddProjectMemberCommandHandler(context, unitOfWork, userContext);
        var command = new AddProjectMemberCommand(project.Id, nonExistentUserId, UserRole.Member);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.NotFoundById(nonExistentUserId));
    }

    [Fact]
    public async Task Handle_Should_ReturnConflict_WhenUserIsAlreadyMember()
    {
        // Arrange
        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(Guid.NewGuid());
        await using var context = CreateDbContext(userContext);

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Existing",
            LastName = "Member",
            Email = "existing@test.com",
            PasswordHash = "hash"
        };
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Test Project",
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            CreatedByUserId = userContext.UserId
        };
        var existingMember = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = user.Id,
            Role = UserRole.Member
        };
        context.Users.Add(user);
        context.Projects.Add(project);
        context.ProjectMembers.Add(existingMember);
        await context.SaveChangesAsync();

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new AddProjectMemberCommandHandler(context, unitOfWork, userContext);
        var command = new AddProjectMemberCommand(project.Id, user.Id, UserRole.TeamLeader);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProjectMemberErrors.AlreadyExists(project.Id, user.Id));
    }

    [Fact]
    public async Task Handle_Should_AddMember_WhenValid()
    {
        // Arrange
        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(Guid.NewGuid());
        await using var context = CreateDbContext(userContext);

        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "New",
            LastName = "Member",
            Email = "newmember@test.com",
            PasswordHash = "hash"
        };
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Test Project",
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            CreatedByUserId = userContext.UserId
        };
        context.Users.Add(user);
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var unitOfWork = Substitute.For<IUnitOfWork>();
        var handler = new AddProjectMemberCommandHandler(context, unitOfWork, userContext);
        var command = new AddProjectMemberCommand(project.Id, user.Id, UserRole.TeamLeader);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());

        ProjectMember? member = await context.ProjectMembers
            .SingleOrDefaultAsync(m => m.ProjectId == project.Id && m.UserId == user.Id);

        member.Should().NotBeNull();
        member!.Role.Should().Be(UserRole.TeamLeader);
    }
}
