using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.ProjectMembers.RemoveProjectMember;

internal sealed class RemoveProjectMemberCommandHandler(
    IApplicationDbContext context,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<RemoveProjectMemberCommand>
{
    public async Task<Result> Handle(
        RemoveProjectMemberCommand command,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure(UserErrors.Unauthorized);
        }

        bool projectExists = await context.Projects
            .AnyAsync(p => p.Id == command.ProjectId, cancellationToken);

        if (!projectExists)
        {
            return Result.Failure(ProjectErrors.NotFound(command.ProjectId));
        }

        if (!userContext.IsSystemAdmin)
        {
            ProjectMember? callerMember = await context.ProjectMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(pm => pm.ProjectId == command.ProjectId && pm.UserId == userContext.UserId.Value, cancellationToken);

            if (callerMember is null)
            {
                return Result.Failure(ProjectMemberErrors.NotProjectMember);
            }

            if ((int)callerMember.Role > (int)UserRole.ProjectManager)
            {
                return Result.Failure(ProjectMemberErrors.Forbidden);
            }
        }

        ProjectMember? member = await context.ProjectMembers
            .SingleOrDefaultAsync(m => m.ProjectId == command.ProjectId && m.UserId == command.UserId, cancellationToken);

        if (member is null)
        {
            return Result.Failure(ProjectMemberErrors.NotFound(command.ProjectId, command.UserId));
        }

        context.ProjectMembers.Remove(member);

        await context.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
