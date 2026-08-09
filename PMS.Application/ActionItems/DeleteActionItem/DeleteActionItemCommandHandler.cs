using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.ActionItems;
using PMS.Domain.ActualExecutions;
using PMS.Domain.PlannedSchedules;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.ActionItems.DeleteActionItem;

internal sealed class DeleteActionItemCommandHandler(
    IApplicationDbContext context,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<DeleteActionItemCommand>
{
    public async Task<Result> Handle(
        DeleteActionItemCommand command,
        CancellationToken cancellationToken)
    {
        // ── 1. Auth Check ──────────────────────────────────────────────────
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure(UserErrors.Unauthorized);
        }

        Guid userId = userContext.UserId.Value;

        // ── 2. Project Existence Check ─────────────────────────────────────
        Project? project = await context.Projects
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == command.ProjectId, cancellationToken);

        if (project is null)
        {
            return Result.Failure(ProjectErrors.NotFound(command.ProjectId));
        }

        // ── 3. ActionItem Existence Check ─────────────────────────────────
        ActionItem? actionItem = await context.ActionItems
            .SingleOrDefaultAsync(a => a.Id == command.ActionItemId && a.ProjectId == command.ProjectId, cancellationToken);

        if (actionItem is null)
        {
            return Result.Failure(ActionItemErrors.NotFound(command.ActionItemId));
        }

        // ── 4. Authorization Check (Min Role: TeamLead or Project Creator) ─
        if (!userContext.IsSystemAdmin)
        {
            ProjectMember? member = await context.ProjectMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(pm => pm.ProjectId == command.ProjectId && pm.UserId == userId, cancellationToken);

            if (member is null)
            {
                return Result.Failure(ActionItemErrors.NotProjectMember);
            }

            bool isCreator = project.CreatedByUserId == userId;
            bool isAuthorizedRole = member.Role is UserRole.Admin or UserRole.ProjectManager or UserRole.TeamLeader;

            if (!isCreator && !isAuthorizedRole)
            {
                return Result.Failure(ActionItemErrors.Forbidden);
            }
        }

        // ── 5. Cascade Delete Children & ActionItem ───────────────────────
        PlannedSchedule? schedule = await context.PlannedSchedules
            .SingleOrDefaultAsync(s => s.ActionItemId == actionItem.Id, cancellationToken);

        if (schedule is not null)
        {
            context.PlannedSchedules.Remove(schedule);
        }

        ActualExecution? execution = await context.ActualExecutions
            .SingleOrDefaultAsync(a => a.ActionItemId == actionItem.Id, cancellationToken);

        if (execution is not null)
        {
            context.ActualExecutions.Remove(execution);
        }

        actionItem.Raise(new ActionItemDeletedDomainEvent(actionItem.Id, actionItem.ProjectId));

        context.ActionItems.Remove(actionItem);

        await context.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
