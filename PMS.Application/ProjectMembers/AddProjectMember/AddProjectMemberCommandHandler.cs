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
    IUserContext userContext)
    : ICommandHandler<AddProjectMemberCommand>
{
    public async Task<Result> Handle(
        AddProjectMemberCommand command,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated)
        {
            return Result.Failure(UserErrors.Unauthorized);
        }

        bool projectExists = await context.Projects
            .AnyAsync(p => p.Id == command.ProjectId, cancellationToken);

        if (!projectExists)
        {
            return Result.Failure(ProjectErrors.NotFound(command.ProjectId));
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
            JoinedAt = DateTimeOffset.UtcNow
        };

        await context.ProjectMembers.AddAsync(member, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
