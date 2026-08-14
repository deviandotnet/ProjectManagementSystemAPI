using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.Calendar.CalculateWorkingDays;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.Calendar;

internal sealed class CalculateWorkingDays : IApiEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/projects/{projectId:guid}/calendar/working-days", async (
            Guid projectId,
            DateOnly startDate,
            DateOnly endDate,
            IQueryHandler<CalculateWorkingDaysQuery, WorkingDaysResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new CalculateWorkingDaysQuery(projectId, startDate, endDate);

            Result<WorkingDaysResponse> result = await handler.Handle(query, cancellationToken);

            return result.Match(
                data => Results.Ok(data),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("Calculate Working Days")
        .WithDescription("Calculates working days, calendar days, weekends, and holidays between two dates for a project.")
        .WithTags(Tags.Calendar);
    }
}
