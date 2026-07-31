using PMS.Application.Abstractions.Messaging;
using PMS.SharedKernel;

namespace PMS.Application.Projects.CreateProject;

public sealed record CreateProjectCommand(
    string Name,
    string? Description,
    DateOnly StartDate,
    DateOnly EndDate,
    int WeekStartDay = 1,
    TimelineScale DefaultTimelineScale = TimelineScale.Weekly,
    ProgressMode ProgressMode = ProgressMode.CountBased,
    Guid? CreatedByUserId = null
) : ICommand<Guid>;
