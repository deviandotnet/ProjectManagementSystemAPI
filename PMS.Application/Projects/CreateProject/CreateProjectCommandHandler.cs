using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions.Authentication;
using PMS.Application.Abstractions.Data;
using PMS.Application.Abstractions.Messaging;
using PMS.Domain.ProjectMembers;
using PMS.Domain.Projects;
using PMS.Domain.Users;
using PMS.SharedKernel;

namespace PMS.Application.Projects.CreateProject;

internal sealed class CreateProjectCommandHandler(
    IApplicationDbContext context,
    IUnitOfWork unitOfWork,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<CreateProjectCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        CreateProjectCommand command,
        CancellationToken cancellationToken)
    {
        if (!userContext.IsAuthenticated || !userContext.UserId.HasValue)
        {
            return Result.Failure<Guid>(UserErrors.Unauthorized);
        }

        bool nameExists = await context.Projects
            .AnyAsync(p => p.Name.ToLower() == command.Name.ToLower(), cancellationToken);

        if (nameExists)
        {
            return Result.Failure<Guid>(ProjectErrors.NameAlreadyExists(command.Name));
        }

        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = command.Name.Trim(),
            Description = command.Description?.Trim(),
            StartDate = command.StartDate,
            EndDate = command.EndDate,
            WeekStartDay = command.WeekStartDay,
            DefaultTimelineScale = command.DefaultTimelineScale,
            ProgressMode = command.ProgressMode,
            Status = ProjectStatus.Active
        };

        project.Raise(new ProjectCreatedDomainEvent(project.Id));

        var projectMember = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = userContext.UserId!.Value,
            Role = UserRole.ProjectManager,
            JoinedAt = dateTimeProvider.UtcNow
        };

        await context.Projects.AddAsync(project, cancellationToken);
        await context.ProjectMembers.AddAsync(projectMember, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return project.Id;
    }
}
