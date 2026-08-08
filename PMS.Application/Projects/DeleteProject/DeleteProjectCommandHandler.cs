using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.Projects.DeleteProject;

internal sealed class DeleteProjectCommandHandler(
    IApplicationDbContext context,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<DeleteProjectCommand>
{
    public async Task<Result> Handle(
        DeleteProjectCommand command,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure(UserErrors.Unauthorized);
        }

        Project? project = await context.Projects
            .SingleOrDefaultAsync(p => p.Id == command.Id, cancellationToken);

        if (project is null)
        {
            return Result.Failure(ProjectErrors.NotFound(command.Id));
        }

        if (!userContext.IsSystemAdmin)
        {
            ProjectMember? member = await context.ProjectMembers
                .AsNoTracking()
                .FirstOrDefaultAsync(pm => pm.ProjectId == command.Id && pm.UserId == userContext.UserId.Value, cancellationToken);

            if (member is null)
            {
                return Result.Failure(ProjectErrors.NotProjectMember);
            }

            bool isCreator = project.CreatedByUserId == userContext.UserId.Value;
            bool isProjectAdmin = member.Role == UserRole.Admin;

            if (!isCreator && !isProjectAdmin)
            {
                return Result.Failure(ProjectErrors.Forbidden);
            }
        }

        project.Raise(new ProjectDeletedDomainEvent(project.Id));

        context.Projects.Remove(project);

        await context.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
