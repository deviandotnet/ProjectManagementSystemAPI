using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.Dashboard.GetDashboard;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.Dashboard;

internal sealed class GetDashboard : IApiEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/dashboard", async (
            int? pageNumber,
            int? pageSize,
            IQueryHandler<GetDashboardQuery, DashboardResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetDashboardQuery(
                pageNumber ?? 1,
                pageSize ?? 20);

            Result<DashboardResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(
                dashboard => Results.Ok(dashboard),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("Get Dashboard KPIs")
        .WithDescription("Retrieves a paginated set of project KPI summary cards (Total, Completed, Ongoing, Delayed, Planned counts, and Progress %) for projects the authenticated user belongs to.")
        .WithTags(Tags.Dashboard);
    }
}
