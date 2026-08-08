using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.Projects.UpdateProject;

internal sealed class UpdateProjectCommandHandler(
    IApplicationDbContext context,
    IUnitOfWork unitOfWork,
    IUserContext userContext)
    : ICommandHandler<UpdateProjectCommand>
{
    public async Task<Result> Handle(
        UpdateProjectCommand command,
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
            bool isProjectManagerOrAdmin = member.Role <= UserRole.ProjectManager;

            if (!isCreator && !isProjectManagerOrAdmin)
            {
                return Result.Failure(ProjectErrors.Forbidden);
            }
        }

        bool nameExists = await context.Projects
            .AnyAsync(p => p.Id != command.Id && p.Name.ToLower() == command.Name.ToLower(), cancellationToken);

        if (nameExists)
        {
            return Result.Failure(ProjectErrors.NameAlreadyExists(command.Name));
        }

        project.Name = command.Name.Trim();
        project.Description = command.Description?.Trim();
        project.StartDate = command.StartDate;
        project.EndDate = command.EndDate;
        project.WeekStartDay = command.WeekStartDay;
        project.DefaultTimelineScale = command.DefaultTimelineScale;
        project.ProgressMode = command.ProgressMode;
        project.Status = command.Status;

        project.Raise(new ProjectUpdatedDomainEvent(project.Id));

        await context.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
