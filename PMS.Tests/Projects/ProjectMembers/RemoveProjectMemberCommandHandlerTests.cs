using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.ProjectMembers.RemoveProjectMember;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.SharedKernel;
using Xunit;

namespace PMS.UnitTests.Projects.ProjectMembers;

public class RemoveProjectMemberCommandHandlerTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenMemberDoesNotExist()
    {
        // Arrange
        await using var context = CreateDbContext();
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var project = new Project
        {
            Id = projectId,
            Name = "Test Project",
            CreatedByUserId = Guid.NewGuid(),
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10))
        };
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(Guid.NewGuid());
        userContext.IsSystemAdmin.Returns(true);
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var handler = new RemoveProjectMemberCommandHandler(context, unitOfWork, userContext);
        var command = new RemoveProjectMemberCommand(projectId, userId);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProjectMemberErrors.NotFound(projectId, userId));
    }

    [Fact]
    public async Task Handle_Should_RemoveMember_WhenMemberExists()
    {
        // Arrange
        await using var context = CreateDbContext();
        var projectId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var project = new Project
        {
            Id = projectId,
            Name = "Test Project",
            CreatedByUserId = Guid.NewGuid(),
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10))
        };
        var member = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            Role = UserRole.Member
        };
        context.Projects.Add(project);
        context.ProjectMembers.Add(member);
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(Guid.NewGuid());
        userContext.IsSystemAdmin.Returns(true);
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var handler = new RemoveProjectMemberCommandHandler(context, unitOfWork, userContext);
        var command = new RemoveProjectMemberCommand(projectId, userId);

        // Act
        Result result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());

        ProjectMember? deletedMember = await context.ProjectMembers.SingleOrDefaultAsync(m => m.Id == member.Id);
        deletedMember.Should().BeNull();
    }
}
