using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.ProjectMembers;
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
        if (!userContext.IsAuthenticated)
        {
            return Result.Failure(UserErrors.Unauthorized);
        }

        ProjectMember? member = await context.ProjectMembers
            .SingleOrDefaultAsync(m => m.ProjectId == command.ProjectId && m.UserId == command.UserId, cancellationToken);

        if (member is null)
        {
            return Result.Failure(ProjectMemberErrors.NotFound(command.ProjectId, command.UserId));
        }

        member.Role = command.Role;

        await context.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
