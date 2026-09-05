using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Dashboard.GetDashboard;
using PMS.Domain.ActionItems;
using PMS.Domain.ActualExecutions;
using PMS.Domain.PlannedSchedules;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.SharedKernel;
using Xunit;

namespace PMS.UnitTests.Dashboard;

public class GetDashboardQueryHandlerTests
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
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();

        var handler = new GetDashboardQueryHandler(context, userContext, dateTimeProvider);
        var query = new GetDashboardQuery();

        // Act
        Result<DashboardResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(UserErrors.Unauthorized);
    }

    [Fact]
    public async Task Handle_Should_ReturnDashboardWithProjects_ForAuthenticatedMember()
    {
        // Arrange
        await using var context = CreateDbContext();
        Guid userId = Guid.NewGuid();
        Guid project1Id = Guid.NewGuid();
        Guid project2Id = Guid.NewGuid();

        var project1 = new Project
        {
            Id = project1Id,
            Name = "Project Alpha",
            ProgressMode = ProgressMode.CountBased,
            Status = ProjectStatus.Active,
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 6, 30),
            CreatedByUserId = userId
        };
        var project2 = new Project
        {
            Id = project2Id,
            Name = "Project Beta",
            ProgressMode = ProgressMode.CountBased,
            Status = ProjectStatus.Active,
            StartDate = new DateOnly(2026, 2, 1),
            EndDate = new DateOnly(2026, 8, 31),
            CreatedByUserId = userId
        };

        var member1 = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project1Id,
            UserId = userId,
            Role = UserRole.ProjectManager
        };
        var member2 = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project2Id,
            UserId = userId,
            Role = UserRole.Member
        };

        // Project 1 Action Item: Completed
        var ai1 = new ActionItem { Id = Guid.NewGuid(), ProjectId = project1Id, CategoryId = Guid.NewGuid(), ActionItemName = "AI 1", Sequence = 1 };
        var ps1 = new PlannedSchedule { Id = Guid.NewGuid(), ActionItemId = ai1.Id, PlannedStartDate = new DateOnly(2026, 1, 1), PlannedEndDate = new DateOnly(2026, 1, 10) };
        var ae1 = new ActualExecution { Id = Guid.NewGuid(), ActionItemId = ai1.Id, ActualStartDate = new DateOnly(2026, 1, 1), ActualEndDate = new DateOnly(2026, 1, 9) };

        context.Projects.AddRange(project1, project2);
        context.ProjectMembers.AddRange(member1, member2);
        context.ActionItems.Add(ai1);
        context.PlannedSchedules.Add(ps1);
        context.ActualExecutions.Add(ae1);
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);
        userContext.IsSystemAdmin.Returns(false);

        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(new DateTime(2026, 1, 15));

        var handler = new GetDashboardQueryHandler(context, userContext, dateTimeProvider);
        var query = new GetDashboardQuery();

        // Act
        Result<DashboardResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Projects.Should().HaveCount(2);

        var p1Summary = result.Value.Projects.Single(p => p.ProjectId == project1Id);
        p1Summary.ProjectName.Should().Be("Project Alpha");
        p1Summary.MyRole.Should().Be("ProjectManager");
        p1Summary.TotalActionItems.Should().Be(1);
        p1Summary.CompletedActionItems.Should().Be(1);
        p1Summary.ProgressPercent.Should().Be(100.0);

        var p2Summary = result.Value.Projects.Single(p => p.ProjectId == project2Id);
        p2Summary.ProjectName.Should().Be("Project Beta");
        p2Summary.MyRole.Should().Be("Member");
        p2Summary.TotalActionItems.Should().Be(0);
        p2Summary.ProgressPercent.Should().Be(0.0);
        result.Value.PageNumber.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
        result.Value.TotalCount.Should().Be(2);
        result.Value.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Should_PageProjectsButCalculateAllItemsForSelectedProject()
    {
        // Arrange
        await using var context = CreateDbContext();
        Guid userId = Guid.NewGuid();
        var projects = new[]
        {
            new Project
            {
                Id = Guid.NewGuid(), Name = "Alpha", StartDate = new DateOnly(2026, 1, 1),
                EndDate = new DateOnly(2026, 12, 31), CreatedByUserId = userId
            },
            new Project
            {
                Id = Guid.NewGuid(), Name = "Beta", StartDate = new DateOnly(2026, 1, 1),
                EndDate = new DateOnly(2026, 12, 31), CreatedByUserId = userId
            },
            new Project
            {
                Id = Guid.NewGuid(), Name = "Gamma", StartDate = new DateOnly(2026, 1, 1),
                EndDate = new DateOnly(2026, 12, 31), CreatedByUserId = userId
            }
        };
        context.Projects.AddRange(projects);
        context.ProjectMembers.AddRange(projects.Select(project => new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = userId,
            Role = UserRole.Member
        }));

        for (int index = 1; index <= 2; index++)
        {
            context.ActionItems.Add(new ActionItem
            {
                Id = Guid.NewGuid(),
                ProjectId = projects[1].Id,
                CategoryId = Guid.NewGuid(),
                ActionItemName = $"Beta Item {index}"
            });
        }

        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(new DateTime(2026, 1, 1));
        var handler = new GetDashboardQueryHandler(context, userContext, dateTimeProvider);
        var query = new GetDashboardQuery(PageNumber: 2, PageSize: 1);

        // Act
        Result<DashboardResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Projects.Should().ContainSingle();
        result.Value.Projects.Single().ProjectName.Should().Be("Beta");
        result.Value.Projects.Single().TotalActionItems.Should().Be(2);
        result.Value.PageNumber.Should().Be(2);
        result.Value.PageSize.Should().Be(1);
        result.Value.TotalCount.Should().Be(3);
        result.Value.TotalPages.Should().Be(3);
        result.Value.HasPreviousPage.Should().BeTrue();
        result.Value.HasNextPage.Should().BeTrue();
    }
}
