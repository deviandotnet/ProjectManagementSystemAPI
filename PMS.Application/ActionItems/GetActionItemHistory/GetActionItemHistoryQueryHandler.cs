using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.ActionItems;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.ActionItems.GetActionItemHistory;

internal sealed class GetActionItemHistoryQueryHandler(
    IApplicationDbContext context,
    IUserContext userContext)
    : IQueryHandler<GetActionItemHistoryQuery, IReadOnlyCollection<ActionItemHistoryResponse>>
{
    public async Task<Result<IReadOnlyCollection<ActionItemHistoryResponse>>> Handle(
        GetActionItemHistoryQuery query,
        CancellationToken cancellationToken)
    {
        // ── 1. Auth Check ──────────────────────────────────────────────────
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure<IReadOnlyCollection<ActionItemHistoryResponse>>(UserErrors.Unauthorized);
        }

        Guid userId = userContext.UserId.Value;

        // ── 2. Project Existence Check ─────────────────────────────────────
        bool projectExists = await context.Projects
            .AnyAsync(p => p.Id == query.ProjectId, cancellationToken);

        if (!projectExists)
        {
            return Result.Failure<IReadOnlyCollection<ActionItemHistoryResponse>>(ProjectErrors.NotFound(query.ProjectId));
        }

        // ── 3. ActionItem Existence Check ─────────────────────────────────
        bool actionItemExists = await context.ActionItems
            .AnyAsync(a => a.Id == query.ActionItemId && a.ProjectId == query.ProjectId, cancellationToken);

        if (!actionItemExists)
        {
            return Result.Failure<IReadOnlyCollection<ActionItemHistoryResponse>>(ActionItemErrors.NotFound(query.ActionItemId));
        }

        // ── 4. Project Membership Check ────────────────────────────────────
        if (!userContext.IsSystemAdmin)
        {
            bool isMember = await context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == query.ProjectId && pm.UserId == userId, cancellationToken);

            if (!isMember)
            {
                return Result.Failure<IReadOnlyCollection<ActionItemHistoryResponse>>(ActionItemErrors.NotProjectMember);
            }
        }

        // ── 5. Query Audit History ─────────────────────────────────────────
        string actionItemIdString = query.ActionItemId.ToString();

        List<ActionItemHistoryResponse> history = await context.AuditLogs
            .AsNoTracking()
            .Where(a => a.EntityName == "ActionItem" && a.EntityId == actionItemIdString)
            .OrderByDescending(a => a.ChangedAt)
            .Select(a => new ActionItemHistoryResponse(
                a.Id,
                a.EntityName,
                a.EntityId,
                a.Action,
                a.FieldName,
                a.OldValue,
                a.NewValue,
                a.ChangedByUserId,
                a.ChangedByName,
                a.ChangedAt))
            .ToListAsync(cancellationToken);

        return history;
    }
}
