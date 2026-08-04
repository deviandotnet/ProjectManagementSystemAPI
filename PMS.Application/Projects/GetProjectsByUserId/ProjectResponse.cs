using System;
using PMS.Domain.Projects;
using PMS.SharedKernel;

namespace PMS.Application.Projects.GetProjectsByUserId;

public sealed record ProjectResponse(
    Guid Id,
    string Name,
    string? Description,
    DateOnly StartDate,
    DateOnly EndDate,
    int WeekStartDay,
    TimelineScale DefaultTimelineScale,
    ProgressMode ProgressMode,
    ProjectStatus Status,
    Guid? CreatedByUserId
);
