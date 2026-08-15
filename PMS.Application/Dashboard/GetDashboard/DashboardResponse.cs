namespace PMS.Application.Dashboard.GetDashboard;

public sealed record DashboardProjectSummaryResponse(
    Guid ProjectId,
    string ProjectName,
    string Status,
    double ProgressPercent,
    int TotalActionItems,
    int CompletedActionItems,
    int OngoingActionItems,
    int DelayedActionItems,
    int PlannedActionItems,
    DateOnly StartDate,
    DateOnly EndDate,
    string MyRole
);

public sealed record DashboardResponse(
    List<DashboardProjectSummaryResponse> Projects
);
