using PMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace PMS.Application.Features.ProjectFeatures.CreateProject
{
    public sealed record CreateProjectRequest(
        string Name,
        string? Description,
        DateOnly StartDate,
        DateOnly EndDate,
        int WeekStartDay,
        TimelineScale DefaultTimelineScale,
        Guid CreatedByUserId


    );

    public sealed record CreateProjectResponse(
        Guid Id,
        string Name,
        string? Description,
        DateOnly StartDate,
        DateOnly EndDate,
        int WeekStartDay,
        TimelineScale DefaultTimelineScale,
        ProjectStatus Status,
        ProgressMode ProgressMode

    );
}
