using PMS.SharedKernel;

namespace PMS.Application.Projects.GetProjectProgress;

public sealed record ProjectProgressResponse(
    Guid ProjectId,
    string ProjectName,
    ProgressMode ProgressMode,
    string ProgressModeLabel,
    double ProgressPercent,
    int TotalActionItems,
    int CompletedActionItems,
    int OngoingActionItems,
    int DelayedActionItems,
    int PlannedActionItems,
    double TotalWeight,
    double CompletedWeight
);
