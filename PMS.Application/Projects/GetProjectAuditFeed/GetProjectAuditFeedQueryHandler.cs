using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.Projects.GetProjectAuditFeed;

internal sealed class GetProjectAuditFeedQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : IQueryHandler<GetProjectAuditFeedQuery, AuditFeedResponse>
{
    public async Task<Result<AuditFeedResponse>> Handle(
        GetProjectAuditFeedQuery query,
        CancellationToken cancellationToken)
    {
        // ── 1. Auth Check ──────────────────────────────────────────────────
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure<AuditFeedResponse>(UserErrors.Unauthorized);
        }

        Guid userId = userContext.UserId.Value;

        // ── 2. Project Existence Check ─────────────────────────────────────
        var project = await context.Projects
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == query.ProjectId, cancellationToken);

        if (project is null)
        {
            return Result.Failure<AuditFeedResponse>(ProjectErrors.NotFound(query.ProjectId));
        }

        // ── 3. Authorization Check (Min Role: TeamLeader or SystemAdmin) ───
        if (!userContext.IsSystemAdmin)
        {
            var membership = await context.ProjectMembers
                .AsNoTracking()
                .SingleOrDefaultAsync(pm => pm.ProjectId == query.ProjectId && pm.UserId == userId, cancellationToken);

            if (membership is null)
            {
                return Result.Failure<AuditFeedResponse>(ProjectErrors.NotProjectMember);
            }

            // Min role: TeamLead (ProjectManager = 1, Admin = 2, TeamLeader = 3, Member = 4, Viewer = 5)
            if (membership.Role is not (UserRole.ProjectManager or UserRole.Admin or UserRole.TeamLeader))
            {
                return Result.Failure<AuditFeedResponse>(ProjectErrors.Forbidden);
            }
        }

        // ── 4. Collect Associated Project Entity IDs & Titles ───────────────
        var entityTitles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [project.Id.ToString()] = project.Name
        };

        // Categories
        var categories = await context.Categories
            .AsNoTracking()
            .Where(c => c.ProjectId == query.ProjectId)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync(cancellationToken);

        foreach (var c in categories)
        {
            entityTitles[c.Id.ToString()] = c.Name;
        }

        var categoryIds = categories.Select(c => c.Id).ToList();

        // SubCategories
        var subCategories = await context.SubCategories
            .AsNoTracking()
            .Where(sc => categoryIds.Contains(sc.CategoryId))
            .Select(sc => new { sc.Id, sc.Name })
            .ToListAsync(cancellationToken);

        foreach (var sc in subCategories)
        {
            entityTitles[sc.Id.ToString()] = sc.Name;
        }

        // Action Items with PlannedSchedules and ActualExecutions
        var actionItems = await (
            from ai in context.ActionItems.AsNoTracking()
            where ai.ProjectId == query.ProjectId
            join ps in context.PlannedSchedules.AsNoTracking() on ai.Id equals ps.ActionItemId into psGroup
            from ps in psGroup.DefaultIfEmpty()
            join ae in context.ActualExecutions.AsNoTracking() on ai.Id equals ae.ActionItemId into aeGroup
            from ae in aeGroup.DefaultIfEmpty()
            select new
            {
                ai.Id,
                ai.ActionItemName,
                PlannedScheduleId = ps != null ? (Guid?)ps.Id : null,
                ActualExecutionId = ae != null ? (Guid?)ae.Id : null
            }
        ).ToListAsync(cancellationToken);

        foreach (var ai in actionItems)
        {
            entityTitles[ai.Id.ToString()] = ai.ActionItemName;

            if (ai.PlannedScheduleId.HasValue)
            {
                entityTitles[ai.PlannedScheduleId.Value.ToString()] = $"{ai.ActionItemName} (Planned Schedule)";
            }

            if (ai.ActualExecutionId.HasValue)
            {
                entityTitles[ai.ActualExecutionId.Value.ToString()] = $"{ai.ActionItemName} (Actual Execution)";
            }
        }

        // Project Members
        var members = await (
            from pm in context.ProjectMembers.AsNoTracking()
            where pm.ProjectId == query.ProjectId
            join u in context.Users.AsNoTracking() on pm.UserId equals u.Id
            select new
            {
                pm.Id,
                MemberName = $"{u.FirstName} {u.LastName}".Trim()
            }
        ).ToListAsync(cancellationToken);

        foreach (var m in members)
        {
            entityTitles[m.Id.ToString()] = m.MemberName;
        }

        var allEntityIds = entityTitles.Keys.ToList();

        // ── 5. Query Audit Logs ─────────────────────────────────────────────
        var auditLogs = await context.AuditLogs
            .AsNoTracking()
            .Where(al => allEntityIds.Contains(al.EntityId))
            .OrderByDescending(al => al.ChangedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        // ── 6. Format Human-Readable Activity Messages ─────────────────────
        var feedItems = auditLogs.Select(log =>
        {
            entityTitles.TryGetValue(log.EntityId, out var title);
            string displayTitle = !string.IsNullOrWhiteSpace(title) ? title : log.EntityName;
            string userName = !string.IsNullOrWhiteSpace(log.ChangedByName) ? log.ChangedByName : "System";
            string formattedDate = log.ChangedAt.ToString("MMM dd, yyyy h:mm tt");

            string activityMessage = FormatActivityMessage(log, displayTitle, userName, formattedDate);

            return new AuditFeedItemResponse(
                log.Id,
                log.EntityName,
                log.EntityId,
                displayTitle,
                log.Action,
                log.FieldName,
                log.OldValue,
                log.NewValue,
                log.ChangedByUserId,
                log.ChangedByName,
                log.ChangedAt,
                activityMessage
            );
        }).ToList();

        return new AuditFeedResponse(project.Id, project.Name, feedItems);
    }

    private static string FormatActivityMessage(
        Domain.AuditLogs.AuditLog log,
        string displayTitle,
        string userName,
        string formattedDate)
    {
        return log.Action switch
        {
            "Create" => $"{userName} created {log.EntityName} '{displayTitle}' on {formattedDate}",
            "Delete" => $"{userName} deleted {log.EntityName} '{displayTitle}' on {formattedDate}",
            "Update" when !string.IsNullOrWhiteSpace(log.FieldName) =>
                $"{userName} changed {log.FieldName} of '{displayTitle}' from '{(string.IsNullOrWhiteSpace(log.OldValue) ? "none" : log.OldValue)}' to '{(string.IsNullOrWhiteSpace(log.NewValue) ? "none" : log.NewValue)}' on {formattedDate}",
            "Update" => $"{userName} updated {log.EntityName} '{displayTitle}' on {formattedDate}",
            _ => $"{userName} modified {log.EntityName} '{displayTitle}' on {formattedDate}"
        };
    }
}
