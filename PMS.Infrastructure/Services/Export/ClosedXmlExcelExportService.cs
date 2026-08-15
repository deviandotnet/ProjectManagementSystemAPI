using ClosedXML.Excel;
using PMS.Application.Abstractions.Export;
using PMS.Domain.ActionItems;

namespace PMS.Infrastructure.Services.Export;

internal sealed class ClosedXmlExcelExportService : IExcelExportService
{
    private static readonly XLColor HeaderBackground = XLColor.FromHtml("#1E293B");
    private static readonly XLColor HeaderFontColor = XLColor.White;
    private static readonly XLColor ZebraStripeColor = XLColor.FromHtml("#F8FAFC");
    private static readonly XLColor PlannedFillColor = XLColor.FromHtml("#CBD5E1");
    private static readonly XLColor BorderColor = XLColor.FromHtml("#E2E8F0");

    public byte[] GenerateProjectExport(ExcelExportData data)
    {
        using var workbook = new XLWorkbook();

        BuildActionItemsSheet(workbook, data);
        BuildTimelineSheet(workbook, data);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private void BuildActionItemsSheet(XLWorkbook workbook, ExcelExportData data)
    {
        var ws = workbook.Worksheets.Add("Action Items");

        var headers = new[]
        {
            "#", "Category", "SubCategory", "Action Item Name", "Priority", "Owner",
            "Plan Start Date", "Plan End Date", "Duration (Calendar Days)",
            "Duration (Working Days)", "Actual Start Date", "Actual End Date",
            "Actual Hours", "Status", "Weight (%)", "Delay Remarks"
        };

        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Fill.BackgroundColor = HeaderBackground;
            cell.Style.Font.FontColor = HeaderFontColor;
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 11;
        }

        ws.SheetView.FreezeRows(1);

        int row = 2;
        foreach (var item in data.ActionItems)
        {
            ws.Cell(row, 1).Value = item.SequenceNumber;
            ws.Cell(row, 2).Value = item.CategoryName;
            ws.Cell(row, 3).Value = item.SubCategoryName;
            ws.Cell(row, 4).Value = item.ActionItemName;
            ws.Cell(row, 5).Value = item.PriorityLabel;
            ws.Cell(row, 6).Value = item.OwnerName;

            ws.Cell(row, 7).Value = item.PlannedStartDate?.ToString("yyyy-MM-dd");
            ws.Cell(row, 8).Value = item.PlannedEndDate?.ToString("yyyy-MM-dd");

            ws.Cell(row, 9).Value = item.DurationCalendarDays;
            ws.Cell(row, 10).Value = item.DurationWorkingDays;

            ws.Cell(row, 11).Value = item.ActualStartDate?.ToString("yyyy-MM-dd");
            ws.Cell(row, 12).Value = item.ActualEndDate?.ToString("yyyy-MM-dd");

            if (item.ActualHours.HasValue) ws.Cell(row, 13).Value = item.ActualHours.Value;
            ws.Cell(row, 14).Value = item.StatusLabel;
            if (item.Weight.HasValue) ws.Cell(row, 15).Value = item.Weight.Value;
            ws.Cell(row, 16).Value = item.DelayRemarks;

            if (row % 2 == 1)
            {
                ws.Range(row, 1, row, headers.Length).Style.Fill.BackgroundColor = ZebraStripeColor;
            }

            row++;
        }

        if (row > 2)
        {
            var dataRange = ws.Range(1, 1, row - 1, headers.Length);
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorderColor = BorderColor;
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.OutsideBorderColor = BorderColor;
        }

        ws.Columns(1, headers.Length).AdjustToContents();
    }

    private void BuildTimelineSheet(XLWorkbook workbook, ExcelExportData data)
    {
        var ws = workbook.Worksheets.Add("Timeline View");

        ws.Cell(1, 1).Value = "Category";
        ws.Cell(1, 2).Value = "SubCategory";
        ws.Cell(1, 3).Value = "Action Item Name";
        ws.Cell(1, 4).Value = "Status";

        ws.Range(1, 1, 1, 4).Style.Fill.BackgroundColor = HeaderBackground;
        ws.Range(1, 1, 1, 4).Style.Font.FontColor = HeaderFontColor;
        ws.Range(1, 1, 1, 4).Style.Font.Bold = true;
        ws.Range(1, 1, 1, 4).Style.Font.FontSize = 11;

        var weekCols = GenerateWeekColumns(data.ProjectStartDate, data.ProjectEndDate, data.WeekStartDay);

        for (int i = 0; i < weekCols.Count; i++)
        {
            int colIndex = i + 5;
            var cell = ws.Cell(1, colIndex);
            cell.Value = weekCols[i].Label;
            cell.Style.Fill.BackgroundColor = HeaderBackground;
            cell.Style.Font.FontColor = HeaderFontColor;
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontSize = 11;
        }

        ws.SheetView.Freeze(1, 4);

        int row = 2;
        string? currentCategory = null;
        int maxCol = 4 + weekCols.Count;

        foreach (var item in data.ActionItems)
        {
            if (item.CategoryName != currentCategory)
            {
                currentCategory = item.CategoryName;
                ws.Cell(row, 1).Value = currentCategory;

                var catColor = XLColor.FromHtml("#3A86FF");
                if (!string.IsNullOrEmpty(item.CategoryColor))
                {
                    try
                    {
                        catColor = XLColor.FromHtml(item.CategoryColor);
                    }
                    catch { }
                }

                var catRange = ws.Range(row, 1, row, maxCol);
                catRange.Style.Fill.BackgroundColor = catColor;
                catRange.Style.Font.FontColor = XLColor.White;
                catRange.Style.Font.Bold = true;

                row++;
            }

            ws.Cell(row, 1).Value = item.CategoryName;
            ws.Cell(row, 2).Value = item.SubCategoryName;
            ws.Cell(row, 3).Value = item.ActionItemName;
            ws.Cell(row, 4).Value = item.StatusLabel;

            for (int w = 0; w < weekCols.Count; w++)
            {
                var week = weekCols[w];
                int col = w + 5;
                var cell = ws.Cell(row, col);

                bool inPlan = DateRangeOverlapsWeek(item.PlannedStartDate, item.PlannedEndDate, week.Start, week.End);
                bool inActual = DateRangeOverlapsWeek(item.ActualStartDate, item.ActualEndDate ?? item.PlannedEndDate ?? week.End, week.Start, week.End);

                if (item.ActualStartDate.HasValue || item.ActualEndDate.HasValue)
                {
                    if (inPlan) cell.Style.Fill.BackgroundColor = PlannedFillColor;
                    if (inActual) cell.Style.Fill.BackgroundColor = GetStatusColor(item.ComputedStatus);
                }
                else
                {
                    if (inPlan)
                    {
                        if (item.ComputedStatus == ActionItemStatus.Plan) cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#AAAAAA");
                        else if (item.ComputedStatus == ActionItemStatus.Delayed) cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F44336");
                        else cell.Style.Fill.BackgroundColor = PlannedFillColor;
                    }
                }
            }
            row++;
        }

        if (row > 2)
        {
            var dataRange = ws.Range(1, 1, row - 1, maxCol);
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorderColor = BorderColor;
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.OutsideBorderColor = BorderColor;
        }

        ws.Columns(1, 4).AdjustToContents();
    }

    private static List<(string Label, DateOnly Start, DateOnly End)> GenerateWeekColumns(
        DateOnly projectStart, DateOnly projectEnd, int weekStartDay)
    {
        var columns = new List<(string Label, DateOnly Start, DateOnly End)>();
        DayOfWeek targetStartDay = (DayOfWeek)(weekStartDay % 7);
        int diff = (7 + (projectStart.DayOfWeek - targetStartDay)) % 7;
        DateOnly currStart = projectStart.AddDays(-diff);
        int weekNum = 1;
        while (currStart <= projectEnd)
        {
            DateOnly currEnd = currStart.AddDays(6);
            string startStr = currStart.ToString("MMM dd");
            string endStr = currEnd.ToString("MMM dd");
            columns.Add(($"WW{weekNum:D2} ({startStr} - {endStr})", currStart, currEnd));
            currStart = currStart.AddDays(7);
            weekNum++;
        }
        return columns;
    }

    private static bool DateRangeOverlapsWeek(
        DateOnly? rangeStart, DateOnly? rangeEnd,
        DateOnly weekStart, DateOnly weekEnd)
    {
        if (!rangeStart.HasValue || !rangeEnd.HasValue) return false;
        return rangeStart.Value <= weekEnd && rangeEnd.Value >= weekStart;
    }

    private static XLColor GetStatusColor(ActionItemStatus status) => status switch
    {
        ActionItemStatus.Plan => XLColor.FromHtml("#AAAAAA"),
        ActionItemStatus.Ongoing => XLColor.FromHtml("#4CAF50"),
        ActionItemStatus.Delayed => XLColor.FromHtml("#F44336"),
        ActionItemStatus.CompletedEarly => XLColor.FromHtml("#2196F3"),
        ActionItemStatus.CompletedOntime => XLColor.FromHtml("#4CAF50"),
        ActionItemStatus.CompletedLate => XLColor.FromHtml("#FFC107"),
        _ => XLColor.FromHtml("#AAAAAA")
    };
}
