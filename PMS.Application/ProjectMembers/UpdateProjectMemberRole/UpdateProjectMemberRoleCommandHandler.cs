using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.ProjectMembers.UpdateProjectMemberRole;

internal sealed class UpdateProjectMemberRoleCommandHandler(
    IApplicationDbContext context,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<UpdateProjectMemberRoleCommand>
{
    public async Task<Result> Handle(
        UpdateProjectMemberRoleCommand command,
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

        member.Raise(new ProjectMemberRoleUpdatedDomainEvent(member.Id, member.ProjectId, member.UserId, command.Role));

        member.Role = command.Role;

        await context.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
