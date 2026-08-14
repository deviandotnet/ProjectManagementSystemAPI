using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.ProjectMembers.AddProjectMember;

internal sealed class AddProjectMemberCommandHandler(
    IApplicationDbContext context,
    IUnitOfWork unitOfWork,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<AddProjectMemberCommand>
{
    public async Task<Result> Handle(
        AddProjectMemberCommand command,
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

        bool userExists = await context.Users
            .AnyAsync(u => u.Id == command.UserId, cancellationToken);

        if (!userExists)
        {
            return Result.Failure(UserErrors.NotFoundById(command.UserId));
        }

        bool alreadyMember = await context.ProjectMembers
            .AnyAsync(m => m.ProjectId == command.ProjectId && m.UserId == command.UserId, cancellationToken);

        if (alreadyMember)
        {
            return Result.Failure(ProjectMemberErrors.AlreadyExists(command.ProjectId, command.UserId));
        }

        var member = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = command.ProjectId,
            UserId = command.UserId,
            Role = command.Role,
            JoinedAt = dateTimeProvider.UtcNow
        };

        member.Raise(new ProjectMemberAddedDomainEvent(member.Id, member.ProjectId, member.UserId));

        await context.ProjectMembers.AddAsync(member, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
