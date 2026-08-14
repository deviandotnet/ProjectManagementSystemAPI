using PMS.Application.Abstractions.Messaging;
using PMS.SharedKernel;

namespace PMS.Application.Projects.GetTimeline;

public sealed record GetTimelineQuery(
    Guid ProjectId,
    TimelineScale? Scale = null,
    DateOnly? StartDate = null,
    DateOnly? EndDate = null
) : IQuery<TimelineResponse>;
