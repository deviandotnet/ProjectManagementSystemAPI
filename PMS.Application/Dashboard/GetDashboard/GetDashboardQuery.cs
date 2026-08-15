using PMS.Application.Abstractions.Messaging;

namespace PMS.Application.Dashboard.GetDashboard;

public sealed record GetDashboardQuery : IQuery<DashboardResponse>;
