using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Projects.GetProjectsByUserId;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.SharedKernel;
using Xunit;

namespace PMS.UnitTests.Projects;

public class GetProjectsByUserIdQueryHandlerTests
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

        var handler = new GetProjectsByUserIdQueryHandler(context, userContext);
        var query = new GetProjectsByUserIdQuery(Guid.NewGuid());

        // Act
        Result<List<ProjectResponse>> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.Unauthorized);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        await using var context = CreateDbContext();
        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);

        var nonExistentUserId = Guid.NewGuid();
        var handler = new GetProjectsByUserIdQueryHandler(context, userContext);
        var query = new GetProjectsByUserIdQuery(nonExistentUserId);

        // Act
        Result<List<ProjectResponse>> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.NotFoundById(nonExistentUserId));
    }

    [Fact]
    public async Task Handle_Should_ReturnEmptyList_WhenUserExistsButHasNoProjects()
    {
        // Arrange
        await using var context = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Alice",
            LastName = "Smith",
            Email = "alice@example.com",
            PasswordHash = "hash"
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);

        var handler = new GetProjectsByUserIdQueryHandler(context, userContext);
        var query = new GetProjectsByUserIdQuery(user.Id);

        // Act
        Result<List<ProjectResponse>> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_Should_ReturnProjects_WhenUserHasProjects()
    {
        // Arrange
        await using var context = CreateDbContext();
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = "Bob",
            LastName = "Jones",
            Email = "bob@example.com",
            PasswordHash = "hash"
        };
        context.Users.Add(user);

        var project1 = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Project One",
            Description = "First test project",
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10)),
            CreatedByUserId = user.Id
        };

        var project2 = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Project Two",
            Description = "Second test project",
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(20)),
            CreatedByUserId = user.Id
        };

        var otherUserProject = new Project
        {
            Id = Guid.NewGuid(),
            Name = "Other Project",
            Description = "Belongs to someone else",
            StartDate = DateOnly.FromDateTime(DateTime.Today),
            EndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            CreatedByUserId = Guid.NewGuid()
        };

        context.Projects.AddRange(project1, project2, otherUserProject);
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);

        var handler = new GetProjectsByUserIdQueryHandler(context, userContext);
        var query = new GetProjectsByUserIdQuery(user.Id);

        // Act
        Result<List<ProjectResponse>> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Select(p => p.Name).Should().Contain(new[] { "Project One", "Project Two" });
    }
}
