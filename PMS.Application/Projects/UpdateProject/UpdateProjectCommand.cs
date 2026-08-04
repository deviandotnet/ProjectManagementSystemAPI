using PMS.Application.Abstractions.Messaging;
using PMS.Domain.Projects;
using PMS.SharedKernel;

namespace PMS.Application.Projects.UpdateProject;

public sealed record UpdateProjectCommand(
    Guid Id,
    string Name,
    string? Description,
    DateOnly StartDate,
    DateOnly EndDate,
    int WeekStartDay,
    TimelineScale DefaultTimelineScale,
    ProgressMode ProgressMode,
    ProjectStatus Status
) : ICommand;
