using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.Holidays.GetHolidays;
using PMS.Domain.HolidayCalendars;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.Holidays;

internal sealed class GetHolidays : IApiEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("api/holidays", async (
            int? year,
            int? type,
            IQueryHandler<GetHolidaysQuery, IReadOnlyCollection<HolidayResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            HolidayType? holidayType = type.HasValue ? (HolidayType)type.Value : null;
            var query = new GetHolidaysQuery(year, holidayType);

            Result<IReadOnlyCollection<HolidayResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(
                holidays => Results.Ok(holidays),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("List Holidays")
        .WithDescription("Retrieves all national, company, and special holidays. Can be filtered by year or type.")
        .WithTags(Tags.Holidays);
    }
}
