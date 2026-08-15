using PMS.Domain.ActionItems;

namespace PMS.Application.Abstractions.Export;

/// <summary>
/// Abstraction for generating Excel workbook exports.
/// Implementation lives in Infrastructure (ClosedXML) to keep Application layer clean.
/// </summary>
public interface IExcelExportService
{
    /// <summary>
    /// Generates a formatted .xlsx workbook containing action item data and a Gantt timeline view.
    /// </summary>
    byte[] GenerateProjectExport(ExcelExportData data);
}

/// <summary>
/// DTO passed from the Application query handler to the Excel service.
/// Contains all the data needed to generate both sheets of the workbook.
/// </summary>
public sealed class ExcelExportData
{
    public required string ProjectName { get; init; }
    public required DateOnly ProjectStartDate { get; init; }
    public required DateOnly ProjectEndDate { get; init; }
    public required int WeekStartDay { get; init; }
    public required List<ExcelActionItemRow> ActionItems { get; init; }
}

public sealed class ExcelActionItemRow
{
    public required int SequenceNumber { get; init; }
    public required string CategoryName { get; init; }
    public required string? CategoryColor { get; init; }
    public required string? SubCategoryName { get; init; }
    public required string ActionItemName { get; init; }
    public required string PriorityLabel { get; init; }
    public required string? OwnerName { get; init; }
    public required DateOnly? PlannedStartDate { get; init; }
    public required DateOnly? PlannedEndDate { get; init; }
    public required int DurationCalendarDays { get; init; }
    public required int DurationWorkingDays { get; init; }
    public required DateOnly? ActualStartDate { get; init; }
    public required DateOnly? ActualEndDate { get; init; }
    public required decimal? ActualHours { get; init; }
    public required ActionItemStatus ComputedStatus { get; init; }
    public required string StatusLabel { get; init; }
    public required decimal? Weight { get; init; }
    public required string? DelayRemarks { get; init; }
}
