using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.ProjectMembers.GetProjectMembers;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.SharedKernel;
using Xunit;

namespace PMS.UnitTests.Projects.ProjectMembers;

public class GetProjectMembersQueryHandlerTests
{
    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Handle_Should_ReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(false);

        var handler = new GetProjectMembersQueryHandler(context, userContext);
        var query = new GetProjectMembersQuery(Guid.NewGuid());

        // Act
        Result<List<ProjectMemberResponse>> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.Unauthorized);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenProjectDoesNotExist()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(Guid.NewGuid());
        userContext.IsSystemAdmin.Returns(true);

        var nonExistentId = Guid.NewGuid();
        var handler = new GetProjectMembersQueryHandler(context, userContext);
        var query = new GetProjectMembersQuery(nonExistentId);

        // Act
        Result<List<ProjectMemberResponse>> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProjectErrors.NotFound(nonExistentId));
    }

    [Fact]
    public async Task Handle_Should_ReturnMembers_WhenProjectExists()
    {
        // Arrange
        await using var context = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Alice",
            LastName = "Manager",
            Email = "alice@test.com",
            PasswordHash = "hash"
        };
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Project With Members",
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            CreatedByUserId = user.Id
        };
        var member = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = user.Id,
            Role = UserRole.ProjectManager,
            JoinedAt = DateTimeOffset.UtcNow
        };
        context.Users.Add(user);
        context.Projects.Add(project);
        context.ProjectMembers.Add(member);
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(Guid.NewGuid());
        userContext.IsSystemAdmin.Returns(true);

        var handler = new GetProjectMembersQueryHandler(context, userContext);
        var query = new GetProjectMembersQuery(project.Id);

        // Act
        Result<List<ProjectMemberResponse>> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(m => m.UserId == user.Id && m.Role == UserRole.ProjectManager);
    }
}
