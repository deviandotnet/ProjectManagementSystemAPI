using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.ActionItems;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.ActionItems.ReorderActionItems;

internal sealed class ReorderActionItemsCommandHandler(
    IApplicationDbContext context,
    IUnitOfWork unitOfWork,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ReorderActionItemsCommand>
{
    public async Task<Result> Handle(
        ReorderActionItemsCommand command,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure(UserErrors.Unauthorized);
        }

        Guid userId = userContext.UserId.Value;

        bool projectExists = await context.Projects
            .AnyAsync(p => p.Id == command.ProjectId, cancellationToken);

        if (!projectExists)
        {
            return Result.Failure(ProjectErrors.NotFound(command.ProjectId));
        }

        if (!userContext.IsSystemAdmin)
        {
            ProjectMember? member = await context.ProjectMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(pm => pm.ProjectId == command.ProjectId && pm.UserId == userId, cancellationToken);

            if (member is null)
            {
                return Result.Failure(ActionItemErrors.NotProjectMember);
            }

            if (member.Role == UserRole.Viewer)
            {
                return Result.Failure(ActionItemErrors.ReadOnlyAccess);
            }
        }

        List<Guid> itemIds = command.Items.Select(i => i.ActionItemId).ToList();

        List<ActionItem> actionItems = await context.ActionItems
            .Where(ai => ai.ProjectId == command.ProjectId && itemIds.Contains(ai.Id))
            .ToListAsync(cancellationToken);

        Dictionary<Guid, int> sequenceMap = command.Items.ToDictionary(i => i.ActionItemId, i => i.Sequence);

        DateTimeOffset now = dateTimeProvider.UtcNow;
        foreach (ActionItem actionItem in actionItems)
        {
            if (sequenceMap.TryGetValue(actionItem.Id, out int newSequence))
            {
                actionItem.Sequence = newSequence;
                actionItem.UpdatedAt = now;
                actionItem.UpdatedByUserId = userId;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
