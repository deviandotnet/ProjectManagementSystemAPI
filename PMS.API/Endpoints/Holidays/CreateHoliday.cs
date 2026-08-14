using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using PMS.API.Endpoints;
using PMS.API.Extensions;
using PMS.Application.Abstractions;
using PMS.Application.Abstractions.Messaging;
using PMS.Application.Holidays.CreateHoliday;
using PMS.Domain.HolidayCalendars;
using PMS.SharedKernel;

namespace PMS.API.Endpoints.Holidays;

internal sealed class CreateHoliday : IApiEndpoint
{
    public sealed record CreateHolidayRequest(
        DateOnly HolidayDate,
        string Name,
        int Type,
        bool IsRecurringAnnually = false,
        int? Year = null
    );

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("api/holidays", async (
            CreateHolidayRequest request,
            ICommandHandler<CreateHolidayCommand, Guid> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new CreateHolidayCommand(
                request.HolidayDate,
                request.Name,
                (HolidayType)request.Type,
                request.IsRecurringAnnually,
                request.Year);

            Result<Guid> result = await handler.Handle(command, cancellationToken);

            return result.Match(
                id => Results.Created($"/api/holidays/{id}", id),
                CustomResults.Problem);
        })
        .RequireAuthorization()
        .WithSummary("Add Custom Holiday")
        .WithDescription("Creates a new company or custom holiday in the global calendar. Requires SystemAdmin.")
        .WithTags(Tags.Holidays);
    }
}
