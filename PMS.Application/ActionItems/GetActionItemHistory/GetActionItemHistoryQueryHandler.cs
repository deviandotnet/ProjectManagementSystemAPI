using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.ActionItems;
using PMS.Domain.AuditLogs;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.ActionItems.GetActionItemHistory;

internal sealed class GetActionItemHistoryQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : IQueryHandler<GetActionItemHistoryQuery, PagedResponse<ActionItemHistoryResponse>>
{
    public async Task<Result<PagedResponse<ActionItemHistoryResponse>>> Handle(
        GetActionItemHistoryQuery query,
        CancellationToken cancellationToken)
    {
        // ── 1. Auth Check ──────────────────────────────────────────────────
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure<PagedResponse<ActionItemHistoryResponse>>(UserErrors.Unauthorized);
        }

        Guid userId = userContext.UserId.Value;

        // ── 2. Project Existence Check ─────────────────────────────────────
        bool projectExists = await context.Projects
            .AnyAsync(p => p.Id == query.ProjectId, cancellationToken);

        if (!projectExists)
        {
            return Result.Failure<PagedResponse<ActionItemHistoryResponse>>(ProjectErrors.NotFound(query.ProjectId));
        }

        // ── 3. ActionItem Existence Check ─────────────────────────────────
        string? itemTitle = await context.ActionItems
            .AsNoTracking()
            .Where(a => a.Id == query.ActionItemId && a.ProjectId == query.ProjectId)
            .Select(a => a.ActionItemName)
            .SingleOrDefaultAsync(cancellationToken);

        if (itemTitle is null)
        {
            return Result.Failure<PagedResponse<ActionItemHistoryResponse>>(ActionItemErrors.NotFound(query.ActionItemId));
        }

        // ── 4. Project Membership Check ────────────────────────────────────
        if (!userContext.IsSystemAdmin)
        {
            bool isMember = await context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == query.ProjectId && pm.UserId == userId, cancellationToken);

            if (!isMember)
            {
                return Result.Failure<PagedResponse<ActionItemHistoryResponse>>(ActionItemErrors.NotProjectMember);
            }
        }

        // ── 5. Query Audit History ─────────────────────────────────────────
        string actionItemIdString = query.ActionItemId.ToString();

        int pageNumber = Math.Max(query.PageNumber, 1);
        int pageSize = Math.Clamp(query.PageSize, 1, 100);
        int skip = (int)Math.Min((long)(pageNumber - 1) * pageSize, int.MaxValue);

        var auditLogsQuery = context.AuditLogs
            .AsNoTracking()
            .Where(a => a.EntityName == "ActionItem" && a.EntityId == actionItemIdString);

        int totalCount = await auditLogsQuery.CountAsync(cancellationToken);

        List<AuditLog> rawLogs = await auditLogsQuery
            .OrderByDescending(a => a.ChangedAt)
            .ThenByDescending(a => a.Id)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        List<ActionItemHistoryResponse> history = rawLogs.Select(log =>
        {
            string userName = !string.IsNullOrWhiteSpace(log.ChangedByName) ? log.ChangedByName : "System";
            string formattedDate = log.ChangedAt.ToString("MMM dd, yyyy h:mm tt");
            string activityMessage = FormatActivityMessage(log, itemTitle, userName, formattedDate);

            return new ActionItemHistoryResponse(
                log.Id,
                log.EntityName,
                log.EntityId,
                log.Action,
                log.FieldName,
                log.OldValue,
                log.NewValue,
                log.ChangedByUserId,
                log.ChangedByName,
                log.ChangedAt,
                activityMessage);
        }).ToList();

        return PagedResponse<ActionItemHistoryResponse>.Create(
            history,
            pageNumber,
            pageSize,
            totalCount);
    }

    private static string FormatActivityMessage(
        AuditLog log,
        string displayTitle,
        string userName,
        string formattedDate)
    {
        return log.Action switch
        {
            "Create" => $"{userName} created ActionItem '{displayTitle}' on {formattedDate}",
            "Delete" => $"{userName} deleted ActionItem '{displayTitle}' on {formattedDate}",
            "Update" when !string.IsNullOrWhiteSpace(log.FieldName) =>
                $"{userName} changed {log.FieldName} of '{displayTitle}' from '{(string.IsNullOrWhiteSpace(log.OldValue) ? "none" : log.OldValue)}' to '{(string.IsNullOrWhiteSpace(log.NewValue) ? "none" : log.NewValue)}' on {formattedDate}",
            "Update" => $"{userName} updated ActionItem '{displayTitle}' on {formattedDate}",
            _ => $"{userName} modified ActionItem '{displayTitle}' on {formattedDate}"
        };
    }
}
