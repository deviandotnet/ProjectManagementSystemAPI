using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Export;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.ActionItems;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.Projects.ExportProjectExcel;

internal sealed class ExportProjectExcelQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider,
    IExcelExportService excelExportService)
    : IQueryHandler<ExportProjectExcelQuery, ExportProjectExcelResponse>
{
    private const string ContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public async Task<Result<ExportProjectExcelResponse>> Handle(
        ExportProjectExcelQuery query,
        CancellationToken cancellationToken)
    {
        // 1. Auth check
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure<ExportProjectExcelResponse>(UserErrors.Unauthorized);
        }

        Guid userId = userContext.UserId.Value;

        // 2. Fetch Project
        Project? project = await context.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == query.ProjectId, cancellationToken);

        if (project is null)
        {
            return Result.Failure<ExportProjectExcelResponse>(ProjectErrors.NotFound(query.ProjectId));
        }

        // 3. Authorization — SystemAdmin or ProjectMember
        if (!userContext.IsSystemAdmin)
        {
            bool isMember = await context.ProjectMembers
                .AsNoTracking()
                .AnyAsync(pm => pm.ProjectId == query.ProjectId && pm.UserId == userId, cancellationToken);

            if (!isMember)
            {
                return Result.Failure<ExportProjectExcelResponse>(ActionItemErrors.NotProjectMember);
            }
        }

        // 4. Load all action items with their categories, subcategories, schedules, and executions
        var rawData = await (
            from ai in context.ActionItems.AsNoTracking()
            where ai.ProjectId == query.ProjectId
            join cat in context.Categories.AsNoTracking() on ai.CategoryId equals cat.Id
            join subCat in context.SubCategories.AsNoTracking() on ai.SubCategoryId equals subCat.Id into subCatGroup
            from subCat in subCatGroup.DefaultIfEmpty()
            join ps in context.PlannedSchedules.AsNoTracking() on ai.Id equals ps.ActionItemId into psGroup
            from ps in psGroup.DefaultIfEmpty()
            join ae in context.ActualExecutions.AsNoTracking() on ai.Id equals ae.ActionItemId into aeGroup
            from ae in aeGroup.DefaultIfEmpty()
            orderby cat.DisplayOrder, subCat.DisplayOrder, ai.Sequence
            select new
            {
                ai.Id,
                ai.ActionItemName,
                ai.Priority,
                ai.OwnerName,
                ai.Weight,
                ai.Remarks,
                ai.Sequence,
                CategoryName = cat.Name,
                CategoryColor = cat.Color,
                CategoryDisplayOrder = cat.DisplayOrder,
                SubCategoryName = subCat != null ? subCat.Name : null,
                SubCategoryDisplayOrder = subCat != null ? (int?)subCat.DisplayOrder : null,
                PlannedStartDate = ps != null ? (DateOnly?)ps.PlannedStartDate : null,
                PlannedEndDate = ps != null ? (DateOnly?)ps.PlannedEndDate : null,
                DurationCalendarDays = ps != null ? ps.DurationCalendarDays : 0,
                DurationWorkingDays = ps != null ? ps.DurationWorkingDays : 0,
                ActualStartDate = ae != null ? ae.ActualStartDate : null,
                ActualEndDate = ae != null ? ae.ActualEndDate : null,
                ActualHours = ae != null ? ae.ActualHours : null,
                DelayReason = ae != null ? ae.DelayReason : null
            }
        ).ToListAsync(cancellationToken);

        // 5. Compute runtime statuses and build export DTOs
        DateOnly today = DateOnly.FromDateTime(dateTimeProvider.UtcNow);
        int sequenceCounter = 1;

        List<ExcelActionItemRow> exportRows = rawData.Select(item =>
        {
            ActionItemStatus status = ActionItemStatusService.ComputeStatus(
                item.PlannedEndDate,
                item.ActualStartDate,
                item.ActualEndDate,
                today);

            return new ExcelActionItemRow
            {
                SequenceNumber = sequenceCounter++,
                CategoryName = item.CategoryName,
                CategoryColor = item.CategoryColor,
                SubCategoryName = item.SubCategoryName,
                ActionItemName = item.ActionItemName,
                PriorityLabel = item.Priority.ToString(),
                OwnerName = item.OwnerName,
                PlannedStartDate = item.PlannedStartDate,
                PlannedEndDate = item.PlannedEndDate,
                DurationCalendarDays = item.DurationCalendarDays,
                DurationWorkingDays = item.DurationWorkingDays,
                ActualStartDate = item.ActualStartDate,
                ActualEndDate = item.ActualEndDate,
                ActualHours = item.ActualHours,
                ComputedStatus = status,
                StatusLabel = FormatStatusLabel(status),
                Weight = item.Weight,
                DelayRemarks = item.DelayReason ?? item.Remarks
            };
        }).ToList();

        // 6. Generate workbook
        var exportData = new ExcelExportData
        {
            ProjectName = project.Name,
            ProjectStartDate = project.StartDate,
            ProjectEndDate = project.EndDate,
            WeekStartDay = project.WeekStartDay,
            ActionItems = exportRows
        };

        byte[] fileBytes = excelExportService.GenerateProjectExport(exportData);

        // 7. Build filename with project title: {ProjectName}_Timeline_{yyyyMMdd}.xlsx
        string sanitizedName = SanitizeFileName(project.Name);
        string dateStamp = today.ToString("yyyyMMdd");
        string fileName = $"{sanitizedName}_Timeline_{dateStamp}.xlsx";

        return new ExportProjectExcelResponse
        {
            FileContent = fileBytes,
            FileName = fileName,
            ContentType = ContentType
        };
    }

    private static string FormatStatusLabel(ActionItemStatus status) => status switch
    {
        ActionItemStatus.Plan => "Plan",
        ActionItemStatus.Ongoing => "Ongoing",
        ActionItemStatus.Delayed => "Delayed",
        ActionItemStatus.CompletedEarly => "Completed Early",
        ActionItemStatus.CompletedOntime => "Completed On Time",
        ActionItemStatus.CompletedLate => "Completed Late",
        _ => "Unknown"
    };

    private static readonly HashSet<char> InvalidFileNameChars =
    [
        '\\', '/', ':', '*', '?', '"', '<', '>', '|',
        .. Path.GetInvalidFileNameChars()
    ];

    private static string SanitizeFileName(string name)
    {
        string stripped = new(name.Where(c => !InvalidFileNameChars.Contains(c) && !char.IsControl(c)).ToArray());
        return stripped.Replace(' ', '_');
    }
}
