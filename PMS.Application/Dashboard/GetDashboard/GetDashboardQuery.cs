using PMS.Application.Abstractions.Messaging;

namespace PMS.Application.Dashboard.GetDashboard;

public sealed record GetDashboardQuery(
    int PageNumber = 1,
    int PageSize = 20)
    : IQuery<DashboardResponse>;
