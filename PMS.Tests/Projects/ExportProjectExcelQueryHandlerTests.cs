using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Export;
using PMS.Application.Projects.ExportProjectExcel;
using PMS.Domain.ActionItems;
using PMS.Domain.ActualExecutions;
using PMS.Domain.Categories;
using PMS.Domain.PlannedSchedules;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.Infrastructure.Database;
using PMS.SharedKernel;
using Xunit;

namespace PMS.UnitTests.Projects;

public class ExportProjectExcelQueryHandlerTests
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
        var excelService = Substitute.For<IExcelExportService>();

        var handler = new ExportProjectExcelQueryHandler(context, userContext, dateTimeProvider, excelService);
        var query = new ExportProjectExcelQuery(Guid.NewGuid());

        // Act
        Result<ExportProjectExcelResponse> result = await handler.Handle(query, CancellationToken.None);

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
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var excelService = Substitute.For<IExcelExportService>();

        var nonExistentProjectId = Guid.NewGuid();
        var handler = new ExportProjectExcelQueryHandler(context, userContext, dateTimeProvider, excelService);
        var query = new ExportProjectExcelQuery(nonExistentProjectId);

        // Act
        Result<ExportProjectExcelResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProjectErrors.NotFound(nonExistentProjectId));
    }

    [Fact]
    public async Task Handle_Should_ReturnNotProjectMember_WhenUserIsNotMember()
    {
        // Arrange
        await using var context = CreateDbContext();
        Guid userId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        var project = new Project
        {
            Id = projectId,
            Name = "Test Project",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 6, 30),
            CreatedByUserId = Guid.NewGuid() // Different user
        };
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);
        userContext.IsSystemAdmin.Returns(false);
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        var excelService = Substitute.For<IExcelExportService>();

        var handler = new ExportProjectExcelQueryHandler(context, userContext, dateTimeProvider, excelService);
        var query = new ExportProjectExcelQuery(projectId);

        // Act
        Result<ExportProjectExcelResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ActionItemErrors.NotProjectMember);
    }

    [Fact]
    public async Task Handle_Should_AllowSystemAdmin_WithoutMembership()
    {
        // Arrange
        await using var context = CreateDbContext();
        Guid userId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        var project = new Project
        {
            Id = projectId,
            Name = "Admin Access Project",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 6, 30),
            CreatedByUserId = Guid.NewGuid()
        };
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);
        userContext.IsSystemAdmin.Returns(true);
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(new DateTime(2026, 3, 15));
        var excelService = Substitute.For<IExcelExportService>();
        excelService.GenerateProjectExport(Arg.Any<ExcelExportData>()).Returns([0x50, 0x4B]);

        var handler = new ExportProjectExcelQueryHandler(context, userContext, dateTimeProvider, excelService);
        var query = new ExportProjectExcelQuery(projectId);

        // Act
        Result<ExportProjectExcelResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    [Fact]
    public async Task Handle_Should_ReturnExcelFile_WithCorrectFilenameAndContent()
    {
        // Arrange
        await using var context = CreateDbContext();
        Guid userId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();

        var project = new Project
        {
            Id = projectId,
            Name = "AI Visualization NG Prediction",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 6, 30),
            WeekStartDay = 1,
            CreatedByUserId = userId
        };
        var member = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            Role = UserRole.ProjectManager
        };
        var category = new Category
        {
            Id = categoryId,
            ProjectId = projectId,
            Name = "Planning",
            DisplayOrder = 1,
            Color = "#3A86FF"
        };

        // Action Item 1: Completed Early
        var ai1 = new ActionItem
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            CategoryId = categoryId,
            ActionItemName = "Requirements Checking",
            Priority = Priority.High,
            OwnerName = "John Doe",
            Sequence = 1,
            Weight = 30
        };
        var ps1 = new PlannedSchedule
        {
            Id = Guid.NewGuid(),
            ActionItemId = ai1.Id,
            PlannedStartDate = new DateOnly(2026, 1, 6),
            PlannedEndDate = new DateOnly(2026, 1, 17),
            DurationCalendarDays = 12,
            DurationWorkingDays = 10
        };
        var ae1 = new ActualExecution
        {
            Id = Guid.NewGuid(),
            ActionItemId = ai1.Id,
            ActualStartDate = new DateOnly(2026, 1, 6),
            ActualEndDate = new DateOnly(2026, 1, 15),
            ActualHours = 40
        };

        // Action Item 2: Ongoing
        var ai2 = new ActionItem
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            CategoryId = categoryId,
            ActionItemName = "UI Mockups",
            Priority = Priority.Medium,
            OwnerName = "Jane Smith",
            Sequence = 2,
            Weight = 20
        };
        var ps2 = new PlannedSchedule
        {
            Id = Guid.NewGuid(),
            ActionItemId = ai2.Id,
            PlannedStartDate = new DateOnly(2026, 1, 20),
            PlannedEndDate = new DateOnly(2026, 2, 14),
            DurationCalendarDays = 26,
            DurationWorkingDays = 20
        };
        var ae2 = new ActualExecution
        {
            Id = Guid.NewGuid(),
            ActionItemId = ai2.Id,
            ActualStartDate = new DateOnly(2026, 1, 20)
        };

        context.Projects.Add(project);
        context.ProjectMembers.Add(member);
        context.Categories.Add(category);
        context.ActionItems.AddRange(ai1, ai2);
        context.PlannedSchedules.AddRange(ps1, ps2);
        context.ActualExecutions.AddRange(ae1, ae2);
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);

        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(new DateTime(2026, 2, 1));

        byte[] fakeExcelBytes = [0x50, 0x4B, 0x03, 0x04]; // ZIP magic bytes
        var excelService = Substitute.For<IExcelExportService>();
        excelService.GenerateProjectExport(Arg.Any<ExcelExportData>()).Returns(fakeExcelBytes);

        var handler = new ExportProjectExcelQueryHandler(context, userContext, dateTimeProvider, excelService);
        var query = new ExportProjectExcelQuery(projectId);

        // Act
        Result<ExportProjectExcelResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FileContent.Should().BeEquivalentTo(fakeExcelBytes);
        result.Value.FileName.Should().Be("AI_Visualization_NG_Prediction_Timeline_20260201.xlsx");
        result.Value.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        // Verify the export service was called with correct data
        excelService.Received(1).GenerateProjectExport(Arg.Is<ExcelExportData>(data =>
            data.ProjectName == "AI Visualization NG Prediction" &&
            data.ActionItems.Count == 2 &&
            data.ActionItems[0].ActionItemName == "Requirements Checking" &&
            data.ActionItems[0].StatusLabel == "Completed Early" &&
            data.ActionItems[1].ActionItemName == "UI Mockups" &&
            data.ActionItems[1].StatusLabel == "Ongoing"
        ));
    }

    [Fact]
    public async Task Handle_Should_ComputeStatusesCorrectly_ForAllStatusTypes()
    {
        // Arrange
        await using var context = CreateDbContext();
        Guid userId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();
        Guid categoryId = Guid.NewGuid();

        var project = new Project
        {
            Id = projectId,
            Name = "Status Test",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 6, 30),
            CreatedByUserId = userId
        };
        var member = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = userId,
            Role = UserRole.Member
        };
        var category = new Category
        {
            Id = categoryId,
            ProjectId = projectId,
            Name = "Test Category",
            DisplayOrder = 1
        };

        // Plan status: no actual dates, planned end in future
        var aiPlan = new ActionItem { Id = Guid.NewGuid(), ProjectId = projectId, CategoryId = categoryId, ActionItemName = "Plan Item", Sequence = 1 };
        var psPlan = new PlannedSchedule { Id = Guid.NewGuid(), ActionItemId = aiPlan.Id, PlannedStartDate = new DateOnly(2026, 3, 1), PlannedEndDate = new DateOnly(2026, 3, 15) };

        // Delayed status: no actual start, planned end in past
        var aiDelayed = new ActionItem { Id = Guid.NewGuid(), ProjectId = projectId, CategoryId = categoryId, ActionItemName = "Delayed Item", Sequence = 2 };
        var psDelayed = new PlannedSchedule { Id = Guid.NewGuid(), ActionItemId = aiDelayed.Id, PlannedStartDate = new DateOnly(2026, 1, 1), PlannedEndDate = new DateOnly(2026, 1, 10) };

        // CompletedLate: actual end after planned end
        var aiLate = new ActionItem { Id = Guid.NewGuid(), ProjectId = projectId, CategoryId = categoryId, ActionItemName = "Late Item", Sequence = 3 };
        var psLate = new PlannedSchedule { Id = Guid.NewGuid(), ActionItemId = aiLate.Id, PlannedStartDate = new DateOnly(2026, 1, 1), PlannedEndDate = new DateOnly(2026, 1, 15) };
        var aeLate = new ActualExecution { Id = Guid.NewGuid(), ActionItemId = aiLate.Id, ActualStartDate = new DateOnly(2026, 1, 1), ActualEndDate = new DateOnly(2026, 1, 20), DelayReason = "Resource unavailable" };

        // CompletedOnTime: actual end == planned end
        var aiOnTime = new ActionItem { Id = Guid.NewGuid(), ProjectId = projectId, CategoryId = categoryId, ActionItemName = "OnTime Item", Sequence = 4 };
        var psOnTime = new PlannedSchedule { Id = Guid.NewGuid(), ActionItemId = aiOnTime.Id, PlannedStartDate = new DateOnly(2026, 1, 1), PlannedEndDate = new DateOnly(2026, 1, 15) };
        var aeOnTime = new ActualExecution { Id = Guid.NewGuid(), ActionItemId = aiOnTime.Id, ActualStartDate = new DateOnly(2026, 1, 1), ActualEndDate = new DateOnly(2026, 1, 15) };

        context.Projects.Add(project);
        context.ProjectMembers.Add(member);
        context.Categories.Add(category);
        context.ActionItems.AddRange(aiPlan, aiDelayed, aiLate, aiOnTime);
        context.PlannedSchedules.AddRange(psPlan, psDelayed, psLate, psOnTime);
        context.ActualExecutions.AddRange(aeLate, aeOnTime);
        await context.SaveChangesAsync();

        var userContext = Substitute.For<IUserContext>();
        userContext.IsAuthenticated.Returns(true);
        userContext.UserId.Returns(userId);

        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(new DateTime(2026, 2, 1)); // Today is Feb 1

        ExcelExportData? capturedData = null;
        var excelService = Substitute.For<IExcelExportService>();
        excelService.GenerateProjectExport(Arg.Do<ExcelExportData>(d => capturedData = d))
            .Returns([0x50, 0x4B]);

        var handler = new ExportProjectExcelQueryHandler(context, userContext, dateTimeProvider, excelService);
        var query = new ExportProjectExcelQuery(projectId);

        // Act
        Result<ExportProjectExcelResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedData.Should().NotBeNull();
        capturedData!.ActionItems.Should().HaveCount(4);

        // Verify status assignments (items ordered by category display order, subcategory display order, then sequence)
        var planItem = capturedData.ActionItems.First(i => i.ActionItemName == "Plan Item");
        planItem.StatusLabel.Should().Be("Plan");

        var delayedItem = capturedData.ActionItems.First(i => i.ActionItemName == "Delayed Item");
        delayedItem.StatusLabel.Should().Be("Delayed");

        var lateItem = capturedData.ActionItems.First(i => i.ActionItemName == "Late Item");
        lateItem.StatusLabel.Should().Be("Completed Late");
        lateItem.DelayRemarks.Should().Be("Resource unavailable");

        var onTimeItem = capturedData.ActionItems.First(i => i.ActionItemName == "OnTime Item");
        onTimeItem.StatusLabel.Should().Be("Completed On Time");
    }

    [Fact]
    public async Task Handle_Should_SanitizeProjectNameInFilename()
    {
        // Arrange
        await using var context = CreateDbContext();
        Guid userId = Guid.NewGuid();
        Guid projectId = Guid.NewGuid();

        var project = new Project
        {
            Id = projectId,
            Name = "Project: AI/ML <v2> Test",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 6, 30),
            CreatedByUserId = userId
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
        userContext.UserId.Returns(userId);

        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(new DateTime(2026, 5, 20));

        var excelService = Substitute.For<IExcelExportService>();
        excelService.GenerateProjectExport(Arg.Any<ExcelExportData>()).Returns([0x50, 0x4B]);

        var handler = new ExportProjectExcelQueryHandler(context, userContext, dateTimeProvider, excelService);
        var query = new ExportProjectExcelQuery(projectId);

        // Act
        Result<ExportProjectExcelResponse> result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Filename should have invalid chars stripped and spaces replaced with underscores
        result.Value.FileName.Should().NotContain(":");
        result.Value.FileName.Should().NotContain("/");
        result.Value.FileName.Should().NotContain("<");
        result.Value.FileName.Should().NotContain(">");
        result.Value.FileName.Should().EndWith("_Timeline_20260520.xlsx");
    }
}
