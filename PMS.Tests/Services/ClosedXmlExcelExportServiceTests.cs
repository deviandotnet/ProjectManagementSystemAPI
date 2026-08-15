using FluentAssertions;
using PMS.Application.Abstractions.Export;
using PMS.Domain.ActionItems;
using PMS.Infrastructure.Services.Export;
using Xunit;

namespace PMS.UnitTests.Services;

public class ClosedXmlExcelExportServiceTests
{
    private readonly ClosedXmlExcelExportService _service = new();

    [Fact]
    public void GenerateProjectExport_Should_ReturnValidExcelBytes_WithEmptyActionItems()
    {
        // Arrange
        var data = new ExcelExportData
        {
            ProjectName = "Empty Project",
            ProjectStartDate = new DateOnly(2026, 1, 1),
            ProjectEndDate = new DateOnly(2026, 3, 31),
            WeekStartDay = 1,
            ActionItems = []
        };

        // Act
        byte[] result = _service.GenerateProjectExport(data);

        // Assert
        result.Should().NotBeEmpty();
        // .xlsx files are ZIP archives and start with PK magic bytes
        result[0].Should().Be(0x50); // 'P'
        result[1].Should().Be(0x4B); // 'K'
    }

    [Fact]
    public void GenerateProjectExport_Should_GenerateTwoSheets()
    {
        // Arrange
        var data = CreateSampleExportData();

        // Act
        byte[] result = _service.GenerateProjectExport(data);

        // Assert
        result.Should().NotBeEmpty();

        // Verify it's a valid workbook by reading it back
        using var stream = new MemoryStream(result);
        using var workbook = new ClosedXML.Excel.XLWorkbook(stream);

        workbook.Worksheets.Count.Should().Be(2);
        workbook.Worksheets.First().Name.Should().Be("Action Items");
        workbook.Worksheets.Last().Name.Should().Be("Timeline View");
    }

    [Fact]
    public void GenerateProjectExport_Should_PopulateActionItemsSheet_WithCorrectHeaders()
    {
        // Arrange
        var data = CreateSampleExportData();

        // Act
        byte[] result = _service.GenerateProjectExport(data);

        // Assert
        using var stream = new MemoryStream(result);
        using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
        var sheet = workbook.Worksheets.First();

        sheet.Cell(1, 1).Value.ToString().Should().Be("#");
        sheet.Cell(1, 2).Value.ToString().Should().Be("Category");
        sheet.Cell(1, 3).Value.ToString().Should().Be("SubCategory");
        sheet.Cell(1, 4).Value.ToString().Should().Be("Action Item Name");
        sheet.Cell(1, 5).Value.ToString().Should().Be("Priority");
        sheet.Cell(1, 6).Value.ToString().Should().Be("Owner");
        sheet.Cell(1, 7).Value.ToString().Should().Be("Plan Start Date");
        sheet.Cell(1, 8).Value.ToString().Should().Be("Plan End Date");
        sheet.Cell(1, 14).Value.ToString().Should().Be("Status");
    }

    [Fact]
    public void GenerateProjectExport_Should_PopulateActionItemsSheet_WithCorrectData()
    {
        // Arrange
        var data = CreateSampleExportData();

        // Act
        byte[] result = _service.GenerateProjectExport(data);

        // Assert
        using var stream = new MemoryStream(result);
        using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
        var sheet = workbook.Worksheets.First();

        // Row 2 = first data row (row 1 is header)
        sheet.Cell(2, 1).Value.ToString().Should().Be("1"); // Sequence
        sheet.Cell(2, 2).Value.ToString().Should().Be("Planning"); // Category
        sheet.Cell(2, 4).Value.ToString().Should().Be("Requirements Checking"); // Action Item Name
        sheet.Cell(2, 5).Value.ToString().Should().Be("High"); // Priority
        sheet.Cell(2, 6).Value.ToString().Should().Be("John Doe"); // Owner
        sheet.Cell(2, 14).Value.ToString().Should().Be("Completed Early"); // Status
    }

    [Fact]
    public void GenerateProjectExport_Should_PopulateTimelineSheet_WithFrozenColumns()
    {
        // Arrange
        var data = CreateSampleExportData();

        // Act
        byte[] result = _service.GenerateProjectExport(data);

        // Assert
        using var stream = new MemoryStream(result);
        using var workbook = new ClosedXML.Excel.XLWorkbook(stream);
        var sheet = workbook.Worksheets.Last();

        // Verify frozen columns A-D exist with headers
        sheet.Cell(1, 1).Value.ToString().Should().Be("Category");
        sheet.Cell(1, 2).Value.ToString().Should().Be("SubCategory");
        sheet.Cell(1, 3).Value.ToString().Should().Be("Action Item Name");
        sheet.Cell(1, 4).Value.ToString().Should().Be("Status");

        // Week columns should start at column 5 (E)
        string weekHeader = sheet.Cell(1, 5).Value.ToString();
        weekHeader.Should().StartWith("WW");
    }

    [Fact]
    public void GenerateProjectExport_Should_HandleMultipleStatusTypes()
    {
        // Arrange
        var data = new ExcelExportData
        {
            ProjectName = "Multi Status Project",
            ProjectStartDate = new DateOnly(2026, 1, 1),
            ProjectEndDate = new DateOnly(2026, 3, 31),
            WeekStartDay = 1,
            ActionItems =
            [
                CreateRow(1, "Plan Task", ActionItemStatus.Plan, "Plan",
                    new DateOnly(2026, 3, 1), new DateOnly(2026, 3, 15), null, null),
                CreateRow(2, "Ongoing Task", ActionItemStatus.Ongoing, "Ongoing",
                    new DateOnly(2026, 1, 10), new DateOnly(2026, 2, 10), new DateOnly(2026, 1, 10), null),
                CreateRow(3, "Delayed Task", ActionItemStatus.Delayed, "Delayed",
                    new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 5), null, null),
                CreateRow(4, "Early Task", ActionItemStatus.CompletedEarly, "Completed Early",
                    new DateOnly(2026, 1, 6), new DateOnly(2026, 1, 20), new DateOnly(2026, 1, 6), new DateOnly(2026, 1, 15)),
                CreateRow(5, "OnTime Task", ActionItemStatus.CompletedOntime, "Completed On Time",
                    new DateOnly(2026, 1, 6), new DateOnly(2026, 1, 20), new DateOnly(2026, 1, 6), new DateOnly(2026, 1, 20)),
                CreateRow(6, "Late Task", ActionItemStatus.CompletedLate, "Completed Late",
                    new DateOnly(2026, 1, 6), new DateOnly(2026, 1, 15), new DateOnly(2026, 1, 6), new DateOnly(2026, 1, 20))
            ]
        };

        // Act
        byte[] result = _service.GenerateProjectExport(data);

        // Assert
        using var stream = new MemoryStream(result);
        using var workbook = new ClosedXML.Excel.XLWorkbook(stream);

        var actionItemsSheet = workbook.Worksheets.First();
        // Should have header + 6 data rows
        actionItemsSheet.LastRowUsed()!.RowNumber().Should().Be(7);

        var timelineSheet = workbook.Worksheets.Last();
        // All 6 action items should appear as rows in the timeline
        timelineSheet.LastRowUsed()!.RowNumber().Should().BeGreaterThanOrEqualTo(7); // header + 6 rows (may include category rows)
    }

    private static ExcelExportData CreateSampleExportData()
    {
        return new ExcelExportData
        {
            ProjectName = "AI Visualization NG Prediction",
            ProjectStartDate = new DateOnly(2026, 1, 1),
            ProjectEndDate = new DateOnly(2026, 6, 30),
            WeekStartDay = 1,
            ActionItems =
            [
                CreateRow(1, "Requirements Checking", ActionItemStatus.CompletedEarly, "Completed Early",
                    new DateOnly(2026, 1, 6), new DateOnly(2026, 1, 17),
                    new DateOnly(2026, 1, 6), new DateOnly(2026, 1, 15)),
                CreateRow(2, "UI Mockups", ActionItemStatus.Ongoing, "Ongoing",
                    new DateOnly(2026, 1, 20), new DateOnly(2026, 2, 14),
                    new DateOnly(2026, 1, 20), null),
                CreateRow(3, "Backend API Setup", ActionItemStatus.Plan, "Plan",
                    new DateOnly(2026, 2, 1), new DateOnly(2026, 3, 15),
                    null, null)
            ]
        };
    }

    private static ExcelActionItemRow CreateRow(
        int seq, string name, ActionItemStatus status, string statusLabel,
        DateOnly? planStart, DateOnly? planEnd,
        DateOnly? actualStart, DateOnly? actualEnd)
    {
        return new ExcelActionItemRow
        {
            SequenceNumber = seq,
            CategoryName = "Planning",
            CategoryColor = "#3A86FF",
            SubCategoryName = null,
            ActionItemName = name,
            PriorityLabel = "High",
            OwnerName = "John Doe",
            PlannedStartDate = planStart,
            PlannedEndDate = planEnd,
            DurationCalendarDays = planStart.HasValue && planEnd.HasValue
                ? planEnd.Value.DayNumber - planStart.Value.DayNumber
                : 0,
            DurationWorkingDays = planStart.HasValue && planEnd.HasValue
                ? (int)((planEnd.Value.DayNumber - planStart.Value.DayNumber) * 5.0 / 7.0)
                : 0,
            ActualStartDate = actualStart,
            ActualEndDate = actualEnd,
            ActualHours = actualEnd.HasValue ? 40m : null,
            ComputedStatus = status,
            StatusLabel = statusLabel,
            Weight = 30,
            DelayRemarks = null
        };
    }
}
