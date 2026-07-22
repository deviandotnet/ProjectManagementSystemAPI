using Microsoft.EntityFrameworkCore;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Data;
using PMS.Domain.Abstractions;
using PMS.Domain.Entities;
using PMS.Domain.Enums;

namespace PMS.Application.Features.ProjectFeatures.CreateProject;

/// <summary>
/// Handler for creating a new project.
/// Validates duplicate name, persists via EF Core, and commits via UnitOfWork.
/// 
/// Request: CreateProjectRequest
/// Response: Result&lt;CreateProjectResponse&gt;
/// </summary>
public sealed class CreateProjectHandler(
    IApplicationDbContext dbContext,
    IUnitOfWork unitOfWork)
    : IHandler<CreateProjectRequest, Result<CreateProjectResponse>>
{
    public async Task<Result<CreateProjectResponse>> HandleAsync(
        CreateProjectRequest command,
        CancellationToken cancellationToken)
    {
        // 1. Check if project with the same name already exists
        bool nameExists = await dbContext.Projects
            .AnyAsync(p => p.Name.ToLower() == command.Name.ToLower(), cancellationToken);

        if (nameExists)
        {
            return ProjectErrors.NameAlreadyExists(command.Name);
        }

        // 2. Create the Project entity
        var project = new Project
        {
            Id = Guid.NewGuid(),
            Name = command.Name.Trim(),
            Description = command.Description?.Trim(),
            StartDate = command.StartDate,
            EndDate = command.EndDate,
            WeekStartDay = command.WeekStartDay,
            DefaultTimelineScale = command.DefaultTimelineScale,
            ProgressMode = ProgressMode.CountBased, // Assuming default progress mode is 1 (e.g., Manual)
            Status = ProjectStatus.Active,
            CreatedByUserId = command.CreatedByUserId
        };

        // 3. Persist entity
        await dbContext.Projects.AddAsync(project, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        // 4. Map to response
        var response = new CreateProjectResponse(
            project.Id,
            project.Name,
            project.Description,
            project.StartDate,
            project.EndDate,
            project.WeekStartDay,
            project.DefaultTimelineScale,
            project.Status,
            project.ProgressMode
            
        );

        return Result.Success(response);
    }
}
